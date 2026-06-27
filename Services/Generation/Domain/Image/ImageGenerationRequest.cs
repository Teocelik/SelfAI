namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// Domain-level image generation isteği. Aspect ratio orchestrator tarafından
/// fal.ai image_size enum'una çevrilip buraya verilir.
/// </summary>
public class ImageGenerationRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string ImageSize { get; set; } = "square_hd";  // fal.ai enum
    public int NumImages { get; set; } = 1;
    public int? Seed { get; set; }
    public int? NumInferenceSteps { get; set; }  // Flux Dev için
    public decimal? GuidanceScale { get; set; }  // Flux Dev için
    public bool EnableSafetyChecker { get; set; } = true;
}
