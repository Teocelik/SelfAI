namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Studio frontend'in JSON gövdesi. Model + prompt + aspect ratio (F.M.3) +
/// opsiyonel Character (F.M.4) / Face Lock / Pose Lock (F.M.6) kişiselleştirme.
/// Character/Face/Pose üçünden en fazla biri set olabilir (mutex — orchestrator doğrular).
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

    // ═══ F.M.6 — Face Lock (PuLID) / F.M.7 — Asset ID ═══
    // FaceAssetId set ise endpoint "fal-ai/pulid-flux"'a override edilir. Backend AssetId'yi
    // sahiplik kontrolüyle URL'e resolve eder (frontend artık ham URL göndermez).
    public Guid? FaceAssetId { get; set; }      // /Assets/Upload?purpose=FaceLock'tan dönen Asset ID
    public decimal? FaceWeight { get; set; }    // PuLID id_weight, default 1.0

    // ═══ F.M.6 — Pose Lock (ControlNet) ═══
    // PoseImageUrl set ise endpoint "fal-ai/flux-controlnet"'e override edilir.
    public string? PoseImageUrl { get; set; }   // /Assets/Upload?purpose=PoseLock'tan dönen fal.ai URL
    public decimal? PoseWeight { get; set; }    // ControlNet scale, default 0.6
}
