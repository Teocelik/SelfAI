using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// Flux Dev — yüksek kalite image generation (ayarlanabilir step + guidance_scale).
/// IFalAiClient üzerinden submit + bekleme yapar; concrete FalAiClient'a bağlı değildir.
/// </summary>
public class FluxDevGenerator : IImageGenerator
{
    public string ModelEndpoint => "fal-ai/flux/dev";
    public decimal EstimatedCostUsd => 0.025m;

    private readonly IFalAiClient _falAiClient;
    private readonly ILogger<FluxDevGenerator> _logger;

    public FluxDevGenerator(IFalAiClient falAiClient, ILogger<FluxDevGenerator> logger)
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

        var payload = new
        {
            prompt = request.Prompt,
            image_size = request.ImageSize,
            num_images = Math.Clamp(request.NumImages, 1, 4),
            num_inference_steps = request.NumInferenceSteps ?? 28,
            guidance_scale = request.GuidanceScale ?? 3.5m,
            enable_safety_checker = request.EnableSafetyChecker,
            seed = request.Seed
        };

        _logger.LogInformation(
            "Flux Dev generation. | Prompt: {PromptPreview} | Steps: {Steps} | Guidance: {Guidance}",
            request.Prompt.Length > 50 ? request.Prompt[..50] + "..." : request.Prompt,
            payload.num_inference_steps, payload.guidance_scale);

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
