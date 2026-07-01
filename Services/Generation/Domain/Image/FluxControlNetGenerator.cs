using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// Flux ControlNet (Pose Lock — F.M.6). Referans görselin pozunu (OpenPose)
/// koruyarak prompt'taki yeni karakteri üretir. Kullanıcı Pose Lock modal'ına
/// poz referansı yükler → GenerationOrchestrator endpoint'i buna override eder.
///
/// Endpoint sabit "fal-ai/flux-controlnet"; control_mode F.M.6'da sabit "openpose"
/// (canny/depth seçimi F.M.UI.1'de eklenecek). Maliyet/tier ModelCatalogEntry'den
/// (DB) okunur. Tek sorumluluk: pose conditioning iş kuralı.
/// </summary>
public class FluxControlNetGenerator
{
    public const string ModelEndpoint = "fal-ai/flux-controlnet";

    private readonly IFalAiClient _falAiClient;
    private readonly ILogger<FluxControlNetGenerator> _logger;

    public FluxControlNetGenerator(IFalAiClient falAiClient, ILogger<FluxControlNetGenerator> logger)
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

        if (string.IsNullOrEmpty(request.PoseImageUrl))
            throw new ArgumentException("Pose reference image URL gerekli.", nameof(request));

        // ControlNet conditioning strength — Pose Lock weight (F.M.6'da frontend sabit 0.6).
        var poseWeight = request.PoseWeight ?? 0.6m;

        var payload = new
        {
            prompt = request.Prompt,
            control_image_url = request.PoseImageUrl,
            control_mode = "openpose",  // F.M.6 sabit; F.M.UI.1'de seçilebilir (canny/depth)
            controlnet_conditioning_scale = poseWeight,
            image_size = request.ImageSize,
            num_images = Math.Clamp(request.NumImages, 1, 4),
            num_inference_steps = request.NumInferenceSteps ?? 28,
            guidance_scale = request.GuidanceScale ?? 3.5m,
            enable_safety_checker = request.EnableSafetyChecker,
            seed = request.Seed
        };

        _logger.LogInformation(
            "Flux ControlNet generation. | PoseUrl: {Url} | Mode: openpose | Weight: {Weight}",
            request.PoseImageUrl,
            poseWeight);

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
