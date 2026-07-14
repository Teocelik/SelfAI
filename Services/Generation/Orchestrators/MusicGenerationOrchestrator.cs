using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.DTOs.Music;
using SelfAI.Entities;
using SelfAI.Hubs;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Domain.Catalog;
using SelfAI.Services.Generation.Domain.Image;
using SelfAI.Services.Generation.Domain.Music;
using SelfAI.Services.Generation.Pricing;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Müzik üretim orchestrator'ı (F.M.10c). GenerationOrchestrator kalıbını izler:
/// kredi pre-charge + log request scope'unda; uzun süren fal.ai çağrıları fire-and-forget
/// background task'ta (yeni DI scope), sonuç SignalR "MusicGenerationUpdate" ile push.
///
/// Maliyet DB-driven (hardcode YOK): ModelCatalogEntry.CostUsd + CatalogTierResolver +
/// CreditPricingService. Müzik endpoint'leri admin panelinden 'text-to-audio' sync ile
/// katalog'a eklenmelidir; katalogda yoksa üretim "tarife yapılandırılmamış" ile reddedilir.
///
/// Kredi TEK işlem: müzik + otomatik albüm kapağı (Ideogram V3) kombine düşülür.
/// </summary>
public class MusicGenerationOrchestrator : IMusicGenerationOrchestrator
{
    // Albüm kapağı endpoint'i — katalogda seed'li (Ideogram V3, tipografi/poster uzmanı).
    private const string CoverEndpoint = "fal-ai/ideogram/v3";

    private readonly AppDbContext _db;
    private readonly ICatalogTierResolver _tierResolver;
    private readonly ICreditPricingService _pricingService;
    private readonly ICreditService _creditService;
    private readonly IGenerationLogService _logService;
    private readonly IHubContext<GenerationHub> _hubContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MusicGenerationOrchestrator> _logger;

    public MusicGenerationOrchestrator(
        AppDbContext db,
        ICatalogTierResolver tierResolver,
        ICreditPricingService pricingService,
        ICreditService creditService,
        IGenerationLogService logService,
        IHubContext<GenerationHub> hubContext,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<MusicGenerationOrchestrator> logger)
    {
        _db = db;
        _tierResolver = tierResolver;
        _pricingService = pricingService;
        _creditService = creditService;
        _logService = logService;
        _hubContext = hubContext;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<ServiceResult<MusicGenerationStartedDto>> StartGenerationAsync(
        MusicGenerationRequest request,
        Guid userId,
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        // 1. Prompt validation (DataAnnotation ötesi güvenlik kapısı)
        if (string.IsNullOrWhiteSpace(request.MusicPrompt) || request.MusicPrompt.Trim().Length < 10)
            return ServiceResult<MusicGenerationStartedDto>.Failure("Müzik prompt'u çok kısa.", 400);

        // 2. Model seçimi mode'a göre
        string musicEndpoint;
        int[] validDurations;
        switch (request.Mode)
        {
            case "backing":
                musicEndpoint = DynamicMusicGenerator.SoniloEndpoint;
                validDurations = new[] { 15, 30, 60 };
                break;
            case "song":
                musicEndpoint = DynamicMusicGenerator.MiniMaxEndpoint;
                validDurations = new[] { 60, 90, 180 };
                break;
            default:
                return ServiceResult<MusicGenerationStartedDto>.Failure(
                    $"Geçersiz mod: {request.Mode}. 'backing' veya 'song' olmalı.", 400);
        }

        // 3. Süre validation mode'a göre
        if (!validDurations.Contains(request.DurationSeconds))
        {
            return ServiceResult<MusicGenerationStartedDto>.Failure(
                $"Geçersiz süre: {request.DurationSeconds}s. Geçerli değerler: {string.Join(", ", validDurations)}",
                400);
        }

        // 4. Kapak aspect ratio whitelist
        if (!IsValidCoverAspect(request.CoverAspectRatio))
        {
            return ServiceResult<MusicGenerationStartedDto>.Failure(
                "Geçersiz kapak formatı.", 400);
        }

        // 5. Kredi hesabı (DB-driven, kombine). Müzik endpoint'i katalogda yoksa üretim reddedilir.
        var musicEntry = await _db.ModelCatalogEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EndpointId == musicEndpoint, cancellationToken);
        var coverEntry = await _db.ModelCatalogEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EndpointId == CoverEndpoint, cancellationToken);

        if (musicEntry == null || coverEntry == null)
        {
            _logger.LogWarning(
                "Music generation tarife eksik. | MusicEndpoint: {Music} ({MusicFound}) | CoverEndpoint: {Cover} ({CoverFound})",
                musicEndpoint, musicEntry != null, CoverEndpoint, coverEntry != null);
            return ServiceResult<MusicGenerationStartedDto>.Failure(
                "Müzik üretim tarifesi yapılandırılmamış. Lütfen daha sonra tekrar deneyin.", 503);
        }

        var musicTier = await _tierResolver.ResolveAsync(musicEndpoint, cancellationToken);
        var coverTier = await _tierResolver.ResolveAsync(CoverEndpoint, cancellationToken);
        var musicCredits = _pricingService.CalculateUserCredits(musicEntry.CostUsd, musicTier);
        var coverCredits = _pricingService.CalculateUserCredits(coverEntry.CostUsd, coverTier);
        var totalCost = musicCredits + coverCredits;

        if (totalCost <= 0)
        {
            _logger.LogWarning(
                "Music generation için kredi hesaplanamadı. | Music: {M} | Cover: {C}",
                musicCredits, coverCredits);
            return ServiceResult<MusicGenerationStartedDto>.Failure(
                "Kredi tarifesi yapılandırılmamış.", 500);
        }

        // 6. Pre-charge — müzik + kapak TEK transaction (kullanıcı için tek işlem hissi).
        var deductResult = await _creditService.TryDeductAsync(
            userId, totalCost,
            $"Müzik üretimi: {request.Mode} ({request.DurationSeconds}s) + albüm kapağı");
        if (!deductResult.IsSuccess)
        {
            _logger.LogWarning(
                "Music generation kredi yetersiz. | UserId: {UserId} | Required: {Required}",
                userId, totalCost);
            return ServiceResult<MusicGenerationStartedDto>.Failure(
                deductResult.Message ?? "Kredi yetersiz.", deductResult.StatusCode);
        }

        // 7. Generation log (Pending). GenerationId string key olarak kullanılır (orchestrator pattern'i).
        var generationId = Guid.NewGuid();
        await _logService.CreateAsync(userId, generationId.ToString(), totalCost, request.MusicPrompt);

        _logger.LogInformation(
            "Music generation started. | UserId: {UserId} | Mode: {Mode} | Duration: {Dur}s | " +
            "Endpoint: {Endpoint} | Credits: {Credits} | GenId: {GenId}",
            userId, request.Mode, request.DurationSeconds, musicEndpoint, totalCost, generationId);

        // 8. Fire-and-forget — uzun süren fal.ai çağrıları (HTTP timeout riski). Yeni DI scope.
        _ = Task.Run(() => GenerateInBackgroundAsync(
            generationId, userId, firebaseUid, request, musicEndpoint, totalCost),
            CancellationToken.None);

        // 9. Hemen response — sonuç SignalR "MusicGenerationUpdate" ile gelecek.
        return ServiceResult<MusicGenerationStartedDto>.Success(new MusicGenerationStartedDto
        {
            GenerationId = generationId,
            Status = "Started",
            Mode = request.Mode,
            CreditsCharged = totalCost,
            Message = "Müzik üretiliyor, tamamlandığında bildirim gelir..."
        }, "Üretim başlatıldı.");
    }

    private async Task GenerateInBackgroundAsync(
        Guid generationId,
        Guid userId,
        string firebaseUid,
        MusicGenerationRequest request,
        string musicEndpoint,
        int totalCost)
    {
        // Background task request scope'unu aşar → scoped servisler için YENİ scope.
        using var scope = _scopeFactory.CreateScope();
        var musicGenerator = scope.ServiceProvider.GetRequiredService<IMusicGenerator>();
        var imageGenerator = scope.ServiceProvider.GetRequiredService<DynamicImageGenerator>();
        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();
        var logService = scope.ServiceProvider.GetRequiredService<IGenerationLogService>();
        var assetStorage = scope.ServiceProvider.GetRequiredService<IAssetStorageProvider>();

        var ct = CancellationToken.None;

        try
        {
            // 1. Müzik üretimi
            var musicInput = new MusicGenerationInput
            {
                Prompt = request.MusicPrompt,
                Lyrics = request.Lyrics,
                DurationSeconds = request.DurationSeconds
            };

            var musicResult = await musicGenerator.GenerateAsync(musicEndpoint, musicInput, ct);
            if (!musicResult.IsSuccess || musicResult.Data == null
                || string.IsNullOrEmpty(musicResult.Data.AudioUrl))
            {
                await HandleFailureAsync(creditService, logService, userId, firebaseUid, request,
                    generationId, totalCost, musicResult.Message ?? "Müzik üretilemedi.");
                return;
            }

            // 2. Audio'yu R2'a taşı (fal.ai CDN URL'i geçici — 24s retention).
            var audioR2Url = await UploadAudioToR2Async(
                assetStorage, musicResult.Data.AudioUrl, musicResult.Data.ContentType,
                userId, generationId, ct);

            if (string.IsNullOrEmpty(audioR2Url))
            {
                await HandleFailureAsync(creditService, logService, userId, firebaseUid, request,
                    generationId, totalCost, "Audio kalıcı depolamaya yüklenemedi.");
                return;
            }

            // 3. Albüm kapağı — music prompt'undan türetilir (Ideogram V3). Başarısız olursa
            //    üretim İPTAL EDİLMEZ; kapaksız (boş URL) devam eder, frontend gradient gösterir.
            var coverUrl = await TryGenerateCoverAsync(imageGenerator, request, generationId, ct);

            // 4. Log complete + media. SaveMediaItemsAsync idempotent (ilk çağrıdan sonra atlar),
            //    bu yüzden audio + kapak TEK çağrıda kaydedilir (Order 0=audio, 1=kapak).
            await logService.UpdateStatusAsync(generationId.ToString(), GenerationStatus.Completed);
            var mediaUrls = string.IsNullOrEmpty(coverUrl)
                ? new[] { audioR2Url }
                : new[] { audioR2Url, coverUrl };
            await logService.SaveMediaItemsAsync(generationId.ToString(), mediaUrls, "audio");

            // 5. SignalR event push
            await NotifyUpdateAsync(request.SignalRConnectionId, firebaseUid, new MusicGenerationResultDto
            {
                GenerationId = generationId,
                Mode = request.Mode,
                AudioUrl = audioR2Url,
                CoverUrl = coverUrl,
                DurationSeconds = request.DurationSeconds,
                CoverAspectRatio = request.CoverAspectRatio,
                MusicPrompt = request.MusicPrompt
            });

            _logger.LogInformation(
                "Music generation completed. | GenId: {Id} | Audio: {Audio} | Cover: {Cover}",
                generationId, audioR2Url, string.IsNullOrEmpty(coverUrl) ? "(yok)" : coverUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Music generation background task hatası. | GenId: {Id}", generationId);
            await HandleFailureAsync(creditService, logService, userId, firebaseUid, request,
                generationId, totalCost, "Beklenmedik bir hata oluştu.");
        }
    }

    // Albüm kapağı üretimi — başarısızlıkta exception yutulur, boş URL döner (üretim iptal olmaz).
    private async Task<string> TryGenerateCoverAsync(
        DynamicImageGenerator imageGenerator, MusicGenerationRequest request,
        Guid generationId, CancellationToken ct)
    {
        try
        {
            var coverRequest = new ImageGenerationRequest
            {
                Prompt = BuildCoverPrompt(request.MusicPrompt, request.Mode),
                NumImages = 1,
                ImageSize = MapAspectRatioToImageSize(request.CoverAspectRatio),
                EnableSafetyChecker = true
            };

            var coverResult = await imageGenerator.GenerateAsync(CoverEndpoint, coverRequest, ct);
            var url = coverResult.Images.FirstOrDefault()?.Url;
            if (!string.IsNullOrEmpty(url))
                return url;

            _logger.LogWarning("Albüm kapağı boş döndü. | GenId: {Id}", generationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Albüm kapağı üretilemedi, müzik kapaksız gönderiliyor. | GenId: {Id}", generationId);
        }
        return string.Empty;
    }

    // fal.ai CDN → R2 kalıcı depolama. contentType/uzantı response'tan türetilir (Sonilo m4a, MiniMax mp3).
    private async Task<string> UploadAudioToR2Async(
        IAssetStorageProvider assetStorage, string falAiUrl, string? contentType,
        Guid userId, Guid generationId, CancellationToken ct)
    {
        try
        {
            var effectiveContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/mpeg" : contentType;
            var extension = ExtensionForContentType(effectiveContentType);

            using var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(falAiUrl, ct);
            response.EnsureSuccessStatusCode();

            await using var audioStream = await response.Content.ReadAsStreamAsync(ct);

            var fileName = $"music/{userId}/{generationId}.{extension}";
            var uploadResult = await assetStorage.UploadAsync(
                audioStream, fileName, effectiveContentType, ct);

            return uploadResult.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio R2 upload hatası. | Url: {Url}", falAiUrl);
            return string.Empty;
        }
    }

    // Fail: kredi iade + log Failed + SignalR "MusicGenerationFailed".
    private async Task HandleFailureAsync(
        ICreditService creditService, IGenerationLogService logService,
        Guid userId, string firebaseUid, MusicGenerationRequest request,
        Guid generationId, int totalCost, string message)
    {
        var ct = CancellationToken.None;
        try
        {
            await creditService.RefundAsync(userId, totalCost, $"Müzik üretimi başarısız: {generationId}", generationId);
            await logService.UpdateStatusAsync(generationId.ToString(), GenerationStatus.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Music failure handling hatası. | GenId: {Id}", generationId);
        }

        await NotifyFailedAsync(request.SignalRConnectionId, firebaseUid, generationId, message);

        _logger.LogWarning(
            "Music generation failed. | GenId: {Id} | UserId: {UserId} | Reason: {Reason}",
            generationId, userId, message);
    }

    // ═══ SignalR push — connectionId primary, yoksa Clients.User(firebaseUid) fallback ═══

    private async Task NotifyUpdateAsync(string? connectionId, string firebaseUid, MusicGenerationResultDto payload)
    {
        if (!string.IsNullOrEmpty(connectionId))
            await _hubContext.Clients.Client(connectionId).SendAsync("MusicGenerationUpdate", payload);
        else
            await _hubContext.Clients.User(firebaseUid).SendAsync("MusicGenerationUpdate", payload);
    }

    private async Task NotifyFailedAsync(string? connectionId, string firebaseUid, Guid generationId, string message)
    {
        var payload = new { generationId = generationId.ToString(), message };
        if (!string.IsNullOrEmpty(connectionId))
            await _hubContext.Clients.Client(connectionId).SendAsync("MusicGenerationFailed", payload);
        else
            await _hubContext.Clients.User(firebaseUid).SendAsync("MusicGenerationFailed", payload);
    }

    // ═══ Yardımcılar ═══

    private static string BuildCoverPrompt(string musicPrompt, string mode)
    {
        var basePrompt = $"album cover artwork for a {mode} track with the following theme: {musicPrompt}";
        var stylePrompt = mode == "backing"
            ? ", minimalist design, abstract aesthetic, clean typography"
            : ", vibrant artistic composition, cinematic, professional album art style";
        return basePrompt + stylePrompt;
    }

    private static bool IsValidCoverAspect(string aspect) =>
        aspect is "1:1" or "9:16" or "16:9" or "4:5";

    // Kapak aspect → fal.ai image_size enum (GenerationOrchestrator mapping'iyle tutarlı).
    private static string MapAspectRatioToImageSize(string aspectRatio)
    {
        return aspectRatio switch
        {
            "1:1" => "square_hd",
            "16:9" => "landscape_16_9",
            "9:16" => "portrait_16_9",
            "4:5" => "portrait_4_3",
            _ => "square_hd"
        };
    }

    private static string ExtensionForContentType(string contentType)
    {
        var ct = contentType.ToLowerInvariant();
        if (ct.Contains("mp4") || ct.Contains("m4a") || ct.Contains("aac")) return "m4a";
        if (ct.Contains("wav")) return "wav";
        if (ct.Contains("ogg")) return "ogg";
        return "mp3"; // audio/mpeg, audio/mp3
    }
}
