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

    // ═══ F.M.4 — Character LoRA alanları (yalnızca FluxLoraGenerator kullanır) ═══
    public string? LoraModelUrl { get; set; }   // Eğitilmiş LoRA model URL'i
    public string? TriggerWord { get; set; }    // Prompt başına eklenir
    public decimal? LoraWeight { get; set; }     // 0.4 (Esnek) / 0.6 (Dengeli) / 0.8 (Güçlü)

    // ═══ F.M.6 — Face Lock alanları (yalnızca FluxPulidGenerator kullanır) ═══
    public string? FaceImageUrl { get; set; }   // fal.ai storage URL (reference_image_url)
    public decimal? FaceWeight { get; set; }    // PuLID id_weight, 0.5–1.5, default 1.0
}
