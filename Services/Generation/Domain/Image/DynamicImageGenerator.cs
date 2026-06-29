using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Domain.Catalog;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// Generic text-to-image generator (F.M.5). Herhangi bir fal.ai text-to-image
/// endpoint'i için çalışır — endpoint string parametre olarak verilir.
/// Common payload (prompt/image_size/num_images/seed) burada kurulur, modele
/// özel default'lar IModelDefaultProvider'dan merge edilir.
///
/// Eski FluxSchnellGenerator/FluxDevGenerator'ın yerini alır. Karakter LoRA
/// inference'ı ayrı domain service'tir (FluxLoraGenerator) — burada karışmaz.
/// </summary>
public class DynamicImageGenerator
{
    private readonly IFalAiClient _falAiClient;
    private readonly IModelDefaultProvider _defaultProvider;
    private readonly ILogger<DynamicImageGenerator> _logger;

    public DynamicImageGenerator(
        IFalAiClient falAiClient,
        IModelDefaultProvider defaultProvider,
        ILogger<DynamicImageGenerator> logger)
    {
        _falAiClient = falAiClient;
        _defaultProvider = defaultProvider;
        _logger = logger;
    }

    public async Task<ImageGenerationResult> GenerateAsync(
        string endpointId,
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
            throw new ArgumentException("Endpoint ID boş olamaz.", nameof(endpointId));

        if (string.IsNullOrWhiteSpace(request.Prompt))
            throw new ArgumentException("Prompt boş olamaz.", nameof(request));

        var numImages = Math.Clamp(request.NumImages, 1, 4);

        // Common payload — her text-to-image modelinde ortak alanlar.
        var payload = new Dictionary<string, object>
        {
            ["prompt"] = request.Prompt,
            ["image_size"] = request.ImageSize,
            ["num_images"] = numImages
        };

        if (request.Seed.HasValue)
            payload["seed"] = request.Seed.Value;

        // Model-spesifik default'lar (Recraft style, Seedream watermark, vb.).
        // Common alanları override etmeyecek şekilde merge edilir.
        var defaults = _defaultProvider.GetDefaultsFor(endpointId);
        foreach (var kv in defaults)
        {
            if (!payload.ContainsKey(kv.Key))
                payload[kv.Key] = kv.Value;
        }

        _logger.LogInformation(
            "Dynamic generation. | Endpoint: {Endpoint} | PromptPreview: {Preview} | NumImages: {N}",
            endpointId,
            request.Prompt.Length > 50 ? request.Prompt[..50] + "..." : request.Prompt,
            numImages);

        var response = await _falAiClient.SubmitAndWaitAsync<FalAiFluxResponse>(
            endpointId, payload, cancellationToken);

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
