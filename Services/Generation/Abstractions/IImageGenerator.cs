using SelfAI.Services.Generation.Domain.Image;

namespace SelfAI.Services.Generation.Abstractions;

// Tek bir image model kategorisi için generation iş mantığı.
public interface IImageGenerator
{
    /// <summary>
    /// fal.ai endpoint ID — örn. "fal-ai/flux/schnell".
    /// Orchestrator hangi generator'ı çağıracağını seçmek için kullanır.
    /// </summary>
    string ModelEndpoint { get; }

    /// <summary>
    /// Modelin fal.ai üzerindeki tahmin edilen USD maliyeti (per image,
    /// 1MP base resolution). CreditPricingService bunu kullanır.
    /// </summary>
    decimal EstimatedCostUsd { get; }

    /// <summary>
    /// Generation'ı submit eder, sonuç gelene kadar bekler, image URL'leri
    /// döner. Hata durumunda FalAiException fırlatır.
    /// </summary>
    Task<ImageGenerationResult> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default);
}
