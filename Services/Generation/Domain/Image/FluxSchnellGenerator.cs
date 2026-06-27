using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// Flux Schnell — hızlı, ucuz image generation (sabit 4 inference step, guidance yok).
/// IFalAiClient üzerinden submit + bekleme yapar; concrete FalAiClient'a bağlı değildir.
/// </summary>
public class FluxSchnellGenerator : IImageGenerator
{
    public string ModelEndpoint => "fal-ai/flux/schnell";
    public decimal EstimatedCostUsd => 0.003m;

    private readonly IFalAiClient _falAiClient;
    private readonly ILogger<FluxSchnellGenerator> _logger;

    public FluxSchnellGenerator(IFalAiClient falAiClient, ILogger<FluxSchnellGenerator> logger)
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

        // Flux Schnell payload — sınırlı parametre seti
        // (num_inference_steps Schnell'de 4 sabit, guidance_scale yok)
        var payload = new
        {
            prompt = request.Prompt,
            image_size = request.ImageSize,
            num_images = Math.Clamp(request.NumImages, 1, 4),
            num_inference_steps = 4,  // Schnell sabit
            enable_safety_checker = request.EnableSafetyChecker,
            seed = request.Seed
        };

        _logger.LogInformation(
            "Flux Schnell generation. | Prompt: {PromptPreview} | ImageSize: {ImageSize} | NumImages: {NumImages}",
            request.Prompt.Length > 50 ? request.Prompt[..50] + "..." : request.Prompt,
            request.ImageSize, payload.num_images);

        var response = await _falAiClient.SubmitAndWaitAsync<FalAiFluxResponse>(
            ModelEndpoint, payload, cancellationToken);

        return MapToResult(response);
    }

    private static ImageGenerationResult MapToResult(FalAiFluxResponse response)
    {
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
