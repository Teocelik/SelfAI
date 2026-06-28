using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Data;
using SelfAI.Entities;
using SelfAI.Entities.Enums;
using SelfAI.Hubs;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Pricing;
using SelfAI.Services.Generation.Providers.FalAi.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.BackgroundServices;

/// <summary>
/// Karakter LoRA training durumunu periyodik kontrol eden background servis (F.M.4).
/// 60s aralık, max 30 dk. COMPLETED → LoRA URL + Ready + SignalR. FAILED/timeout →
/// refund + Failed + SignalR. Singleton + HostedService dual registration: orchestrator
/// RegisterTrainingJob'u doğrudan çağırabilsin diye (GenerationPollingService ile aynı kalıp).
///
/// SignalR bildirimi Clients.User(firebaseUid) ile gider (mevcut FirebaseUserIdProvider
/// kalıbı — group YOK). Bu yüzden context'te firebaseUid taşınır.
/// </summary>
public class LoraTrainingPollingService : BackgroundService
{
    private const int PollingIntervalMs = 60_000;        // 1 dakika
    private const int MaxPollingDurationMinutes = 30;

    private readonly ConcurrentDictionary<Guid, TrainingJobContext> _activeJobs = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoraTrainingPollingService> _logger;

    public LoraTrainingPollingService(
        IServiceProvider serviceProvider,
        ILogger<LoraTrainingPollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>Orchestrator training submit sonrası job'u kaydeder.</summary>
    public void RegisterTrainingJob(Guid characterId, Guid userId, string firebaseUid, string requestId, int creditsCharged)
    {
        _activeJobs[characterId] = new TrainingJobContext
        {
            UserId = userId,
            FirebaseUid = firebaseUid,
            RequestId = requestId,
            CreditsCharged = creditsCharged,
            StartedAt = DateTime.UtcNow
        };
        _logger.LogInformation(
            "Training job kaydedildi. | CharId: {CharId} | JobId: {JobId}",
            characterId, requestId);
    }

    // ═══ Startup recovery (G) — app restart sonrası in-progress training'leri geri yükle ═══
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pricing = scope.ServiceProvider.GetRequiredService<ICreditPricingService>();
            var pricingOptions = scope.ServiceProvider.GetRequiredService<IOptions<CreditPricingOptions>>().Value;

            // Recovery için kredi miktarı deterministik olarak yeniden hesaplanır.
            var creditsCharged = pricing.CalculateUserCredits(
                pricingOptions.CharacterTrainingCostUsd, ModelTier.CharacterLora);

            // Training durumunda + jobId olan karakterler + sahibinin FirebaseUid'i.
            var inProgress = await db.Characters
                .Where(c => c.LoraTrainingStatus == LoraTrainingStatus.Training
                         && c.LoraTrainingJobId != null)
                .Select(c => new
                {
                    c.Id,
                    c.UserId,
                    c.LoraTrainingJobId,
                    c.TrainingStartedAt,
                    FirebaseUid = c.User.FirebaseUid
                })
                .ToListAsync(cancellationToken);

            foreach (var c in inProgress)
            {
                _activeJobs[c.Id] = new TrainingJobContext
                {
                    UserId = c.UserId,
                    FirebaseUid = c.FirebaseUid ?? string.Empty,
                    RequestId = c.LoraTrainingJobId!,
                    CreditsCharged = creditsCharged,
                    StartedAt = c.TrainingStartedAt ?? DateTime.UtcNow
                };
            }

            _logger.LogInformation(
                "Startup recovery: {Count} aktif training job kaydedildi.", inProgress.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup recovery hatası (training job'lar yüklenemedi).");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LoraTrainingPollingService başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_activeJobs.IsEmpty)
                    await PollAllJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Training polling iterasyonu hatası.");
            }

            await Task.Delay(PollingIntervalMs, stoppingToken);
        }
    }

    private async Task PollAllJobsAsync(CancellationToken cancellationToken)
    {
        foreach (var characterId in _activeJobs.Keys.ToList())
        {
            if (!_activeJobs.TryGetValue(characterId, out var ctx)) continue;

            // Timeout kontrolü
            if (DateTime.UtcNow - ctx.StartedAt > TimeSpan.FromMinutes(MaxPollingDurationMinutes))
            {
                await HandleTimeoutAsync(characterId, ctx);
                continue;
            }

            try
            {
                await CheckSingleJobAsync(characterId, ctx, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Training job kontrol hatası. | CharId: {CharId}", characterId);
            }
        }
    }

    private async Task CheckSingleJobAsync(Guid characterId, TrainingJobContext ctx, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var trainer = scope.ServiceProvider.GetRequiredService<ICharacterTrainer>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GenerationHub>>();
        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();

        var status = await trainer.GetTrainingStatusAsync(ctx.RequestId, cancellationToken);

        if (!status.Status.IsTerminal())
            return;  // IN_QUEUE / IN_PROGRESS — devam ediyor

        var character = await db.Characters.FindAsync(new object[] { characterId }, cancellationToken);
        if (character == null)
        {
            _activeJobs.TryRemove(characterId, out _);
            return;
        }

        if (status.Status == FalAiRequestStatus.Completed)
        {
            try
            {
                var loraUrl = await trainer.GetTrainingResultAsync(ctx.RequestId, cancellationToken);
                character.LoraModelUrl = loraUrl;
                character.LoraTrainingStatus = LoraTrainingStatus.Ready;
                character.TrainingCompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Training tamamlandı. | CharId: {CharId} | LoraUrl: {Url}", characterId, loraUrl);

                await hubContext.Clients.User(ctx.FirebaseUid).SendAsync("CharacterTrainingUpdate", new
                {
                    characterId,
                    status = "Ready",
                    name = character.Name,
                    thumbnailUrl = character.ThumbnailUrl
                }, cancellationToken);

                _activeJobs.TryRemove(characterId, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Training result fetch hatası. | CharId: {CharId}", characterId);
                await MarkFailedAsync(character, db, creditService, hubContext, ctx,
                    "Eğitim tamamlandı ancak sonuç alınamadı.", cancellationToken);
            }
        }
        else if (status.Status == FalAiRequestStatus.Failed)
        {
            await MarkFailedAsync(character, db, creditService, hubContext, ctx,
                status.FailureReason != null
                    ? "Eğitim başarısız oldu (görsel kalitesi veya içerik politikası)."
                    : "Eğitim başarısız oldu.",
                cancellationToken);
        }
    }

    private async Task MarkFailedAsync(
        Character character,
        AppDbContext db,
        ICreditService creditService,
        IHubContext<GenerationHub> hubContext,
        TrainingJobContext ctx,
        string reason,
        CancellationToken cancellationToken)
    {
        character.LoraTrainingStatus = LoraTrainingStatus.Failed;
        character.TrainingFailureReason = reason;
        await db.SaveChangesAsync(cancellationToken);

        await creditService.RefundAsync(ctx.UserId, ctx.CreditsCharged,
            $"Character training failed: {character.Id}");

        _logger.LogWarning(
            "Training başarısız. | CharId: {CharId} | Reason: {Reason}", character.Id, reason);

        await hubContext.Clients.User(ctx.FirebaseUid).SendAsync("CharacterTrainingUpdate", new
        {
            characterId = character.Id,
            status = "Failed",
            name = character.Name,
            reason = $"{reason} Krediniz iade edildi."
        }, cancellationToken);

        _activeJobs.TryRemove(character.Id, out _);
    }

    private async Task HandleTimeoutAsync(Guid characterId, TrainingJobContext ctx)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GenerationHub>>();

        var character = await db.Characters.FindAsync(characterId);
        if (character != null)
        {
            await MarkFailedAsync(character, db, creditService, hubContext, ctx,
                "Eğitim 30 dakikada tamamlanamadı (zaman aşımı).", CancellationToken.None);
        }
        else
        {
            _activeJobs.TryRemove(characterId, out _);
        }
    }

    private class TrainingJobContext
    {
        public Guid UserId { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public int CreditsCharged { get; set; }
        public DateTime StartedAt { get; set; }
    }
}
