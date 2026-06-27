using Microsoft.AspNetCore.SignalR;
using SelfAI.Entities;
using SelfAI.Hubs;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Domain.Image;
using SelfAI.Services.Generation.Pricing;
using SelfAI.Services.Generation.Providers.FalAi;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Image generation iş akışı koordinatörü. Kredi pre-charge + history kaydını
/// request scope'unda yapar, fal.ai çağrısını fire-and-forget background task'ta
/// (yeni DI scope ile) yürütür ve sonucu SignalR "GenerationUpdate" ile push eder.
///
/// NOT: Background task request scope'unu aşar; scoped servisler (ICreditService,
/// IGenerationLogService → AppDbContext) IServiceScopeFactory ile YENİ scope'tan
/// alınır. IHubContext singleton olduğu için doğrudan kullanılabilir.
/// </summary>
public class GenerationOrchestrator : IGenerationOrchestrator
{
    private readonly IEnumerable<IImageGenerator> _imageGenerators;
    private readonly ICreditPricingService _pricingService;
    private readonly ICreditService _creditService;
    private readonly IGenerationLogService _logService;
    private readonly IHubContext<GenerationHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GenerationOrchestrator> _logger;

    public GenerationOrchestrator(
        IEnumerable<IImageGenerator> imageGenerators,
        ICreditPricingService pricingService,
        ICreditService creditService,
        IGenerationLogService logService,
        IHubContext<GenerationHub> hubContext,
        IServiceScopeFactory scopeFactory,
        ILogger<GenerationOrchestrator> logger)
    {
        _imageGenerators = imageGenerators;
        _pricingService = pricingService;
        _creditService = creditService;
        _logService = logService;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
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

        // 2. Generator lookup (metadata için; gerçek çağrı background scope'ta yeniden çözülür)
        var generator = _imageGenerators.FirstOrDefault(g => g.ModelEndpoint == request.ModelEndpoint);
        if (generator == null)
        {
            _logger.LogWarning(
                "Bilinmeyen model endpoint. | Endpoint: {Endpoint} | UserId: {UserId}",
                request.ModelEndpoint, userId);
            return ServiceResult<GenerationStartedResponse>.Failure("Seçilen model bulunamadı.", 400);
        }

        // 3. Credit calculation
        var tier = _pricingService.MapEndpointToTier(generator.ModelEndpoint);
        var creditsRequired = _pricingService.CalculateUserCredits(generator.EstimatedCostUsd, tier);

        // 4. Credit deduction (pre-charge — başarısızlıkta refund). TryDeductAsync atomiktir.
        var deductionResult = await _creditService.TryDeductAsync(
            userId, creditsRequired, $"Generation: {generator.ModelEndpoint}");
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
            userId, creditsRequired, generator.ModelEndpoint);

        // 5. History kaydı (Pending status'üyle). Tracking ID string key olarak kullanılır.
        var generationId = Guid.NewGuid();
        await _logService.CreateAsync(userId, generationId.ToString(), creditsRequired, request.Prompt);

        // 6. Fire-and-forget — background task ile fal.ai çağrısı (request scope'u aşar)
        _ = Task.Run(() => ExecuteGenerationBackgroundAsync(
            request, userId, firebaseUid, generationId, creditsRequired, signalRConnectionId),
            CancellationToken.None);

        // 7. Hemen response dön — sonuç SignalR ile gelecek
        return ServiceResult<GenerationStartedResponse>.Success(new GenerationStartedResponse
        {
            GenerationId = generationId,
            Status = "Processing",
            CreditsCharged = creditsRequired
        }, "Üretim başlatıldı.");
    }

    private async Task ExecuteGenerationBackgroundAsync(
        StartGenerationRequest request,
        Guid userId,
        string firebaseUid,
        Guid generationId,
        int creditsCharged,
        string? signalRConnectionId)
    {
        // Background task request scope'unu aşar → scoped servisler için YENİ scope.
        using var scope = _scopeFactory.CreateScope();
        var generators = scope.ServiceProvider.GetRequiredService<IEnumerable<IImageGenerator>>();
        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();
        var logService = scope.ServiceProvider.GetRequiredService<IGenerationLogService>();

        try
        {
            var generator = generators.FirstOrDefault(g => g.ModelEndpoint == request.ModelEndpoint)
                ?? throw new FalAiException($"Generator bulunamadı: {request.ModelEndpoint}");

            var imageSize = MapAspectRatioToImageSize(request.AspectRatio);

            var genRequest = new ImageGenerationRequest
            {
                Prompt = request.Prompt,
                ImageSize = imageSize,
                NumImages = request.NumImages,
                Seed = request.Seed,
                NumInferenceSteps = request.NumInferenceSteps,
                GuidanceScale = request.GuidanceScale,
                EnableSafetyChecker = true
            };

            var result = await generator.GenerateAsync(genRequest);

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
