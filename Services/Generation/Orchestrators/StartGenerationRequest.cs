namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Studio frontend'in JSON gövdesi. Sadece model + prompt + aspect ratio
/// (F.M.3 scope). Character/Face/Pose alanları F.M.4/F.M.6'da eklenecek.
/// </summary>
public class StartGenerationRequest
{
    public string ModelEndpoint { get; set; } = string.Empty;  // örn. "fal-ai/flux/schnell"
    public string Prompt { get; set; } = string.Empty;
    public string AspectRatio { get; set; } = "1:1";  // "1:1", "16:9", "9:16", "4:5", "3:2", "2:3"
    public int NumImages { get; set; } = 1;
    public int? Seed { get; set; }
    public int? NumInferenceSteps { get; set; }
    public decimal? GuidanceScale { get; set; }

    // ═══ F.M.4 — Character LoRA seçimi ═══
    // CharacterId set ise backend ModelEndpoint'i "fal-ai/flux-lora"'ya override eder.
    public Guid? CharacterId { get; set; }
    public string? CharacterMode { get; set; }  // "flexible" / "balanced" / "strong"
}
