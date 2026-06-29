using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.Entities;
using SelfAI.Entities.Enums;
using SelfAI.Hubs;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Domain.Catalog;
using SelfAI.Services.Generation.Domain.Image;
using SelfAI.Services.Generation.Pricing;
using SelfAI.Services.Generation.Providers.FalAi;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Image generation iş akışı koordinatörü (F.M.5 — dynamic catalog).
///
/// Model seçimi artık iki yola ayrılır:
///   • Karakter seçili değil → DynamicImageGenerator (generic, endpoint string'iyle)
///   • Karakter seçili        → FluxLoraGenerator (karakter LoRA inference)
///
/// Maliyet/tier hardcoded değil, ModelCatalogEntry (DB) + ICatalogTierResolver'dan
/// gelir. Kredi pre-charge + history request scope'unda; fal.ai çağrısı fire-and-forget
/// background task'ta (yeni DI scope) yürütülür, sonuç SignalR "GenerationUpdate" ile push.
/// </summary>
public class GenerationOrchestrator : IGenerationOrchestrator
{
    private readonly ICatalogTierResolver _tierResolver;
    private readonly ICreditPricingService _pricingService;
    private readonly ICreditService _creditService;
    private readonly IGenerationLogService _logService;
    private readonly IHubContext<GenerationHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppDbContext _db;
    private readonly ILogger<GenerationOrchestrator> _logger;

    public GenerationOrchestrator(
        ICatalogTierResolver tierResolver,
        ICreditPricingService pricingService,
        ICreditService creditService,
        IGenerationLogService logService,
        IHubContext<GenerationHub> hubContext,
        IServiceScopeFactory scopeFactory,
        AppDbContext db,
        ILogger<GenerationOrchestrator> logger)
    {
        _tierResolver = tierResolver;
        _pricingService = pricingService;
        _creditService = creditService;
        _logService = logService;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<GenerationStartedResponse>> StartGenerationAsync(
        StartGenerationRequest request,
        Guid userId,
        string firebaseUid,
        string? signalRConnectionId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return ServiceResult<GenerationStartedResponse>.Failure("Prompt boş olamaz.", 400);

        if (string.IsNullOrWhiteSpace(request.ModelEndpoint))
            return ServiceResult<GenerationStartedResponse>.Failure("Model seçilmedi.", 400);

        // 2. Karakter çözümü (F.M.4) — CharacterId set ise endpoint LoRA'ya override edilir.
        var effectiveEndpoint = request.ModelEndpoint;
        var isCharacterPath = false;
        string? loraModelUrl = null;
        string? triggerWord = null;
        decimal? loraWeight = null;

        if (request.CharacterId.HasValue)
        {
            var character = await _db.Characters.FirstOrDefaultAsync(
                c => c.Id == request.CharacterId.Value
                  && c.UserId == userId
                  && c.Status == CharacterStatus.Active,
                cancellationToken);

            if (character == null)
                return ServiceResult<GenerationStartedResponse>.Failure("Karakter bulunamadı.", 404);

            if (character.LoraTrainingStatus != LoraTrainingStatus.Ready
                || string.IsNullOrEmpty(character.LoraModelUrl))
                return ServiceResult<GenerationStartedResponse>.Failure("Karakter henüz hazır değil.", 409);

            // Kullanıcı hangi modeli seçerse seçsin, karakter varsa LoRA endpoint'i kullanılır.
            effectiveEndpoint = FluxLoraGenerator.LoraEndpoint;
            isCharacterPath = true;
            loraModelUrl = character.LoraModelUrl;
            triggerWord = character.TriggerWord;
            loraWeight = MapCharacterModeToWeight(request.CharacterMode);

            _logger.LogInformation(
                "Karakter ile generation. | CharId: {CharId} | Mode: {Mode} | Weight: {Weight}",
                character.Id, request.CharacterMode ?? "balanced", loraWeight);
        }

        // 3. Catalog lookup — endpoint DB'de var mı? Maliyet/tier DB'den okunur.
        //    Karakter yolu Hidden flux-lora kaydını kullanır (kullanıcıya görünmez ama
        //    cost lookup için seed'lidir); doğrudan kullanıcı seçimi Approved olmalıdır.
        var entry = await _db.ModelCatalogEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EndpointId == effectiveEndpoint, cancellationToken);

        if (entry == null)
        {
            _logger.LogWarning(
                "Catalog'da olmayan endpoint. | Endpoint: {Endpoint} | UserId: {UserId}",
                effectiveEndpoint, userId);
            return ServiceResult<GenerationStartedResponse>.Failure("Geçersiz model.", 400);
        }

        if (!isCharacterPath && entry.Status != CatalogStatus.Approved)
        {
            _logger.LogWarning(
                "Onaylı olmayan model seçildi. | Endpoint: {Endpoint} | Status: {Status}",
                effectiveEndpoint, entry.Status);
            return ServiceResult<GenerationStartedResponse>.Failure("Bu model şu an kullanılamıyor.", 400);
        }

        // 4. Tier + credit hesabı (ikisi de DB-driven, hardcoded değil)
        var tier = await _tierResolver.ResolveAsync(effectiveEndpoint, cancellationToken);
        var creditsRequired = _pricingService.CalculateUserCredits(entry.CostUsd, tier);

        // 5. Credit deduction (pre-charge — başarısızlıkta refund). TryDeductAsync atomiktir.
        var deductionResult = await _creditService.TryDeductAsync(
            userId, creditsRequired, $"Generation: {effectiveEndpoint}");
        if (!deductionResult.IsSuccess)
        {
            _logger.LogWarning(
                "Kredi yetersiz. | UserId: {UserId} | Required: {Required}",
                userId, creditsRequired);
            return ServiceResult<GenerationStartedResponse>.Failure(
                deductionResult.Message ?? "Kredi yetersiz.", deductionResult.StatusCode);
        }

        _logger.LogInformation(
            "Kredi düşüldü. | UserId: {UserId} | Amount: {Credits} | Endpoint: {Endpoint}",
            userId, creditsRequired, effectiveEndpoint);

        // 6. History kaydı (Pending status'üyle). Tracking ID string key olarak kullanılır.
        var generationId = Guid.NewGuid();
        await _logService.CreateAsync(userId, generationId.ToString(), creditsRequired, request.Prompt);

        // 7. Fire-and-forget — background task ile fal.ai çağrısı (request scope'u aşar)
        _ = Task.Run(() => ExecuteGenerationBackgroundAsync(
            request, effectiveEndpoint, isCharacterPath, loraModelUrl, triggerWord, loraWeight,
            userId, firebaseUid, generationId, creditsRequired, signalRConnectionId),
            CancellationToken.None);

        // 8. Hemen response dön — sonuç SignalR ile gelecek
        return ServiceResult<GenerationStartedResponse>.Success(new GenerationStartedResponse
        {
            GenerationId = generationId,
            Status = "Processing",
            CreditsCharged = creditsRequired
        }, "Üretim başlatıldı.");
    }

    private async Task ExecuteGenerationBackgroundAsync(
        StartGenerationRequest request,
        string effectiveEndpoint,
        bool isCharacterPath,
        string? loraModelUrl,
        string? triggerWord,
        decimal? loraWeight,
        Guid userId,
        string firebaseUid,
        Guid generationId,
        int creditsCharged,
        string? signalRConnectionId)
    {
        // Background task request scope'unu aşar → scoped servisler için YENİ scope.
        using var scope = _scopeFactory.CreateScope();
        var dynamicGenerator = scope.ServiceProvider.GetRequiredService<DynamicImageGenerator>();
        var loraGenerator = scope.ServiceProvider.GetRequiredService<FluxLoraGenerator>();
        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();
        var logService = scope.ServiceProvider.GetRequiredService<IGenerationLogService>();

        try
        {
            var imageSize = MapAspectRatioToImageSize(request.AspectRatio);

            var genRequest = new ImageGenerationRequest
            {
                Prompt = request.Prompt,
                ImageSize = imageSize,
                NumImages = request.NumImages,
                Seed = request.Seed,
                NumInferenceSteps = request.NumInferenceSteps,
                GuidanceScale = request.GuidanceScale,
                EnableSafetyChecker = true,
                // LoRA alanları yalnızca karakter yolunda dolu.
                LoraModelUrl = loraModelUrl,
                TriggerWord = triggerWord,
                LoraWeight = loraWeight
            };

            // Karakter yolu → FluxLoraGenerator; aksi halde → DynamicImageGenerator (endpoint string'iyle).
            var result = isCharacterPath
                ? await loraGenerator.GenerateAsync(genRequest)
                : await dynamicGenerator.GenerateAsync(effectiveEndpoint, genRequest);

            // Success — history güncelle (status + media)
            await logService.UpdateStatusAsync(generationId.ToString(), GenerationStatus.Completed);
            await logService.SaveMediaItemsAsync(
                generationId.ToString(), result.Images.Select(i => i.Url), "image");

            _logger.LogInformation(
                "Generation tamamlandı. | GenerationId: {GenId} | UserId: {UserId} | ImageCount: {Count}",
                generationId, userId, result.Images.Count);

            await NotifyClientAsync(signalRConnectionId, firebaseUid, generationId, "Completed", result.Images);
        }
        catch (FalAiException falEx)
        {
            _logger.LogError(falEx,
                "fal.ai generation hatası. | GenerationId: {GenId} | UserId: {UserId}",
                generationId, userId);

            await RefundAndMarkFailedAsync(creditService, logService, userId, creditsCharged, generationId);
            await NotifyClientAsync(signalRConnectionId, firebaseUid, generationId, "Failed",
                errorMessage: "Üretim başarısız. Krediniz iade edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Generation beklenmedik hata. | GenerationId: {GenId} | UserId: {UserId}",
                generationId, userId);

            await RefundAndMarkFailedAsync(creditService, logService, userId, creditsCharged, generationId);
            await NotifyClientAsync(signalRConnectionId, firebaseUid, generationId, "Failed",
                errorMessage: "Beklenmedik bir hata oluştu. Krediniz iade edildi.");
        }
    }

    private async Task RefundAndMarkFailedAsync(
        ICreditService creditService, IGenerationLogService logService,
        Guid userId, int creditsCharged, Guid generationId)
    {
        await creditService.RefundAsync(userId, creditsCharged, $"Generation failed: {generationId}");
        await logService.UpdateStatusAsync(generationId.ToString(), GenerationStatus.Failed);
    }

    // Character mode pill → LoRA weight (F.M.4). Esnek/Dengeli/Güçlü.
    private static decimal MapCharacterModeToWeight(string? mode)
    {
        return mode switch
        {
            "flexible" => 0.4m,
            "balanced" => 0.6m,
            "strong" => 0.8m,
            _ => 0.6m  // Dengeli default
        };
    }

    // Aspect ratio → fal.ai image_size enum mapping (en yakın enum'a yuvarlanır)
    private static string MapAspectRatioToImageSize(string aspectRatio)
    {
        return aspectRatio switch
        {
            "1:1" => "square_hd",
            "16:9" => "landscape_16_9",
            "9:16" => "portrait_16_9",
            "4:3" => "landscape_4_3",
            "3:4" => "portrait_4_3",
            "3:2" => "landscape_4_3",
            "2:3" => "portrait_4_3",
            "4:5" => "portrait_4_3",
            _ => "square_hd"
        };
    }

    /// <summary>
    /// SignalR push: önce connectionId (primary), yoksa Clients.User(firebaseUid)
    /// fallback (FirebaseUserIdProvider sayesinde kullanıcının tüm sekmelerine yayar).
    /// </summary>
    private async Task NotifyClientAsync(
        string? signalRConnectionId,
        string firebaseUid,
        Guid generationId,
        string status,
        List<GeneratedImage>? images = null,
        string? errorMessage = null)
    {
        var payload = new
        {
            generationId = generationId.ToString(),
            status,
            images = images?.Select(i => new { i.Url, i.Width, i.Height }).ToList(),
            errorMessage
        };

        if (!string.IsNullOrEmpty(signalRConnectionId))
        {
            await _hubContext.Clients.Client(signalRConnectionId).SendAsync("GenerationUpdate", payload);
        }
        else
        {
            await _hubContext.Clients.User(firebaseUid).SendAsync("GenerationUpdate", payload);
        }
    }
}
