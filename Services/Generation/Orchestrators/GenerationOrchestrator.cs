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
    private readonly IAssetService _assetService;
    private readonly IGenerationLogService _logService;
    private readonly IHubContext<GenerationHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppDbContext _db;
    private readonly ILogger<GenerationOrchestrator> _logger;

    public GenerationOrchestrator(
        ICatalogTierResolver tierResolver,
        ICreditPricingService pricingService,
        ICreditService creditService,
        IAssetService assetService,
        IGenerationLogService logService,
        IHubContext<GenerationHub> hubContext,
        IServiceScopeFactory scopeFactory,
        AppDbContext db,
        ILogger<GenerationOrchestrator> logger)
    {
        _tierResolver = tierResolver;
        _pricingService = pricingService;
        _creditService = creditService;
        _assetService = assetService;
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

        // 2. Kişiselleştirme mutex (F.M.6) — Character / Face Lock / Pose Lock üçünden
        //    en fazla biri aktif olabilir. Üçü de tek bir effectiveEndpoint'e route edildiği
        //    için birden fazlası anlamsız; backend güvenlik kapısı olarak reddeder.
        var hasCharacter = request.CharacterId.HasValue;
        var hasFace = request.FaceAssetId.HasValue;
        var hasPose = !string.IsNullOrWhiteSpace(request.PoseImageUrl);

        // Feature aktifse endpoint override edilir (Character/Face/Pose); model seçimi
        // ZORUNLU DEĞİL. Yalnızca hiçbir feature yokken (generic yol) model gerekir.
        if (!hasCharacter && !hasFace && !hasPose
            && string.IsNullOrWhiteSpace(request.ModelEndpoint))
            return ServiceResult<GenerationStartedResponse>.Failure("Model seçilmedi.", 400);

        var activeFeatures = (hasCharacter ? 1 : 0) + (hasFace ? 1 : 0) + (hasPose ? 1 : 0);
        if (activeFeatures > 1)
        {
            _logger.LogWarning(
                "Birden fazla kişiselleştirme aynı anda seçildi. | Char: {C} | Face: {F} | Pose: {P} | UserId: {UserId}",
                hasCharacter, hasFace, hasPose, userId);
            return ServiceResult<GenerationStartedResponse>.Failure(
                "Aynı anda sadece bir kişiselleştirme seçilebilir (Karakter, Face Lock veya Pose Lock).", 400);
        }

        // 3. Endpoint çözümü — feature aktifse kullanıcının model seçimi OVERRIDE edilir.
        //    Character→flux-lora, Face→pulid-flux, Pose→flux-controlnet. Bu üç endpoint
        //    catalog'da Pending (kullanıcıya gizli) olarak seed'lidir; cost/tier lookup için
        //    DB'de bulunur ama isInternalPath sayesinde Approved guard'ından muaf tutulur.
        var effectiveEndpoint = request.ModelEndpoint;
        var isInternalPath = hasCharacter || hasFace || hasPose;
        string? loraModelUrl = null;
        string? triggerWord = null;
        decimal? loraWeight = null;
        string? faceImageUrl = null;  // F.M.7 — AssetId'den resolve edilen URL (aşağıda doldurulur)

        if (hasCharacter)
        {
            var character = await _db.Characters.FirstOrDefaultAsync(
                c => c.Id == request.CharacterId!.Value
                  && c.UserId == userId
                  && c.Status == CharacterStatus.Active,
                cancellationToken);

            if (character == null)
                return ServiceResult<GenerationStartedResponse>.Failure("Karakter bulunamadı.", 404);

            if (character.LoraTrainingStatus != LoraTrainingStatus.Ready
                || string.IsNullOrEmpty(character.LoraModelUrl))
                return ServiceResult<GenerationStartedResponse>.Failure("Karakter henüz hazır değil.", 409);

            effectiveEndpoint = FluxLoraGenerator.LoraEndpoint;
            loraModelUrl = character.LoraModelUrl;
            triggerWord = character.TriggerWord;
            loraWeight = MapCharacterModeToWeight(request.CharacterMode);

            _logger.LogInformation(
                "Karakter ile generation. | CharId: {CharId} | Mode: {Mode} | Weight: {Weight}",
                character.Id, request.CharacterMode ?? "balanced", loraWeight);
        }
        else if (hasFace)
        {
            effectiveEndpoint = FluxPulidGenerator.ModelEndpoint;

            // F.M.7 — AssetId → URL resolve + sahiplik kontrolü. Kredi düşülmeden ÖNCE yapılır;
            // asset kullanıcıya ait değilse/silinmişse burada 403 döner, kredi düşmez (Senaryo 5).
            var faceUrlResult = await _assetService.ResolveUrlAsync(
                request.FaceAssetId!.Value, userId, cancellationToken);
            if (!faceUrlResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Face asset resolve edilemedi. | AssetId: {AssetId} | UserId: {UserId}",
                    request.FaceAssetId, userId);
                return ServiceResult<GenerationStartedResponse>.Failure(
                    faceUrlResult.Message, faceUrlResult.StatusCode);
            }
            faceImageUrl = faceUrlResult.Data;

            _logger.LogInformation(
                "Face Lock ile generation. | Endpoint: {Endpoint} | UserId: {UserId}",
                effectiveEndpoint, userId);
        }
        else if (hasPose)
        {
            effectiveEndpoint = FluxControlNetGenerator.ModelEndpoint;
            _logger.LogInformation(
                "Pose Lock ile generation. | Endpoint: {Endpoint} | UserId: {UserId}",
                effectiveEndpoint, userId);
        }

        // 4. Catalog lookup — endpoint DB'de var mı? Maliyet/tier DB'den okunur.
        //    Internal path (character/face/pose) Pending kaydı kullanır (kullanıcıya görünmez
        //    ama cost lookup için seed'lidir); doğrudan kullanıcı seçimi Approved olmalıdır.
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

        if (!isInternalPath && entry.Status != CatalogStatus.Approved)
        {
            _logger.LogWarning(
                "Onaylı olmayan model seçildi. | Endpoint: {Endpoint} | Status: {Status}",
                effectiveEndpoint, entry.Status);
            return ServiceResult<GenerationStartedResponse>.Failure("Bu model şu an kullanılamıyor.", 400);
        }

        // 5. Tier + credit hesabı (ikisi de DB-driven, hardcoded değil)
        var tier = await _tierResolver.ResolveAsync(effectiveEndpoint, cancellationToken);
        var creditsRequired = _pricingService.CalculateUserCredits(entry.CostUsd, tier);

        // 6. Credit deduction (pre-charge — başarısızlıkta refund). TryDeductAsync atomiktir.
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

        // 7. History kaydı (Pending status'üyle). Tracking ID string key olarak kullanılır.
        var generationId = Guid.NewGuid();
        await _logService.CreateAsync(userId, generationId.ToString(), creditsRequired, request.Prompt);

        // 8. Fire-and-forget — background task ile fal.ai çağrısı (request scope'u aşar)
        _ = Task.Run(() => ExecuteGenerationBackgroundAsync(
            request, effectiveEndpoint, loraModelUrl, triggerWord, loraWeight, faceImageUrl,
            userId, firebaseUid, generationId, creditsRequired, signalRConnectionId),
            CancellationToken.None);

        // 9. Hemen response dön — sonuç SignalR ile gelecek
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
        string? loraModelUrl,
        string? triggerWord,
        decimal? loraWeight,
        string? faceImageUrl,
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
        var pulidGenerator = scope.ServiceProvider.GetRequiredService<FluxPulidGenerator>();
        var controlNetGenerator = scope.ServiceProvider.GetRequiredService<FluxControlNetGenerator>();
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
                LoraWeight = loraWeight,
                // Face/Pose alanları yalnızca ilgili yolda dolu (F.M.6/F.M.7).
                // FaceImageUrl request scope'unda AssetId'den resolve edildi.
                FaceImageUrl = faceImageUrl,
                FaceWeight = request.FaceWeight,
                PoseImageUrl = request.PoseImageUrl,
                PoseWeight = request.PoseWeight
            };

            // effectiveEndpoint'e göre domain generator seçilir (orchestrator step 3'te override edildi).
            //   flux-lora → FluxLoraGenerator, pulid-flux → FluxPulidGenerator,
            //   flux-controlnet → FluxControlNetGenerator, aksi halde generic DynamicImageGenerator.
            ImageGenerationResult result;
            switch (effectiveEndpoint)
            {
                case FluxLoraGenerator.LoraEndpoint:
                    result = await loraGenerator.GenerateAsync(genRequest);
                    break;
                case FluxPulidGenerator.ModelEndpoint:
                    result = await pulidGenerator.GenerateAsync(genRequest);
                    break;
                case FluxControlNetGenerator.ModelEndpoint:
                    result = await controlNetGenerator.GenerateAsync(genRequest);
                    break;
                default:
                    result = await dynamicGenerator.GenerateAsync(effectiveEndpoint, genRequest);
                    break;
            }

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
