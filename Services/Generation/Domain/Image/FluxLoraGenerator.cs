using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// Flux LoRA inference (F.M.4) — eğitilmiş karakter LoRA'sıyla image generation.
/// Trigger word prompt başına eklenir, LoRA ağırlığı mode'a göre (0.4/0.6/0.8) ayarlanır.
/// GenerationOrchestrator karakter seçiliyse endpoint'i buna override eder.
///
/// F.M.5: Artık IImageGenerator implement etmez (interface kaldırıldı). Karaktere
/// özel logic'i generic DynamicImageGenerator'dan ayrı tutmak için kendi class'ı olarak
/// durur. Endpoint sabit "fal-ai/flux-lora"; maliyet/tier ModelCatalogEntry'den okunur.
/// </summary>
public class FluxLoraGenerator
{
    public const string LoraEndpoint = "fal-ai/flux-lora";

    private readonly IFalAiClient _falAiClient;
    private readonly ILogger<FluxLoraGenerator> _logger;

    public FluxLoraGenerator(IFalAiClient falAiClient, ILogger<FluxLoraGenerator> logger)
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

        if (string.IsNullOrEmpty(request.LoraModelUrl))
            throw new ArgumentException("LoRA model URL gerekli.", nameof(request));

        var loraWeight = request.LoraWeight ?? 0.6m;  // Dengeli default

        // Trigger word'ü prompt başına ekle (karakter LoRA'sının tetiklenmesi için).
        var prefixedPrompt = !string.IsNullOrEmpty(request.TriggerWord)
            ? $"{request.TriggerWord}, {request.Prompt}"
            : request.Prompt;

        var payload = new
        {
            prompt = prefixedPrompt,
            image_size = request.ImageSize,
            num_images = Math.Clamp(request.NumImages, 1, 4),
            num_inference_steps = request.NumInferenceSteps ?? 28,
            guidance_scale = request.GuidanceScale ?? 3.5m,
            enable_safety_checker = request.EnableSafetyChecker,
            seed = request.Seed,
            loras = new[]
            {
                new { path = request.LoraModelUrl, scale = loraWeight }
            }
        };

        _logger.LogInformation(
            "Flux LoRA generation. | LoraUrl: {Url} | Weight: {Weight} | TriggerWord: {TW}",
            request.LoraModelUrl, loraWeight, request.TriggerWord);

        var response = await _falAiClient.SubmitAndWaitAsync<FalAiFluxResponse>(
            LoraEndpoint, payload, cancellationToken);

        return new ImageGenerationResult
        {
            Images = response.Images.Select(i => new GeneratedImage
            {
                Url = i.Url,
                Width = i.Width ?? 0,
                Height = i.Height ?? 0,
                ContentType = i.ContentType
            }).ToList(),
            Seed = response.Seed,
            InferenceTimeSeconds = response.Timings?.Inference
        };
    }
}
