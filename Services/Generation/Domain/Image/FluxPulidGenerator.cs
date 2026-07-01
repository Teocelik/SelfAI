using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// PuLID Flux (Face Lock — F.M.6). Tek bir yüz referans görseliyle karakter
/// tutarlılığı (identity transfer) sağlar. Kullanıcı Face Lock paneline yüz
/// görseli yükler → GenerationOrchestrator endpoint'i buna override eder.
///
/// Endpoint sabit "fal-ai/flux-pulid"; maliyet/tier ModelCatalogEntry'den (DB)
/// okunur. Karakter LoRA / generic generation'dan ayrı domain service'tir
/// (Single Responsibility) — yalnızca face conditioning iş kuralını bilir.
/// </summary>
public class FluxPulidGenerator
{
    public const string ModelEndpoint = "fal-ai/flux-pulid";

    private readonly IFalAiClient _falAiClient;
    private readonly ILogger<FluxPulidGenerator> _logger;

    public FluxPulidGenerator(IFalAiClient falAiClient, ILogger<FluxPulidGenerator> logger)
    {
        _falAiClient = falAiClient;
        _logger = logger;
    }

    public async Task<ImageGenerationResult> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt boş olamaz.", nameof(request));

        if (string.IsNullOrEmpty(request.FaceImageUrl))
            throw new ArgumentException("Face image URL gerekli.", nameof(request));

        // PuLID identity strength — Face Lock weight (F.M.6'da frontend sabit 1.0 yollar).
        var idWeight = request.FaceWeight ?? 1.0m;

        // fal.ai official schema (fal-ai/flux-pulid): tek görsel döner — num_images YOK.
        // Frontend NumImages > 1 gönderse bile response tek görsel içerir.
        var payload = new
        {
            prompt = request.Prompt,
            reference_image_url = request.FaceImageUrl,
            image_size = request.ImageSize,
            num_inference_steps = request.NumInferenceSteps ?? 20,
            guidance_scale = request.GuidanceScale ?? 4.0m,
            id_weight = idWeight,
            negative_prompt = "",  // Boş; kullanıcı gelecekte ayarlayabilir (F.M.UI.1)
            enable_safety_checker = request.EnableSafetyChecker,
            seed = request.Seed
        };

        _logger.LogInformation(
            "PuLID Flux generation. | FaceUrl: {Url} | IdWeight: {Weight} | PromptPreview: {Preview}",
            request.FaceImageUrl,
            idWeight,
            request.Prompt.Length > 50 ? request.Prompt[..50] + "..." : request.Prompt);

        var response = await _falAiClient.SubmitAndWaitAsync<FalAiFluxResponse>(
            ModelEndpoint, payload, cancellationToken);

        return new ImageGenerationResult
        {
            Images = response.Images.Select(i => new GeneratedImage
            {
                Url = i.Url,
                Width = i.Width,
                Height = i.Height,
                ContentType = i.ContentType
            }).ToList(),
            Seed = response.Seed,
            InferenceTimeSeconds = response.Timings?.Inference
        };
    }
}
