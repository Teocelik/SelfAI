namespace SelfAI.DTOs.Templates;

/// <summary>
/// Preset generation tamamlandıktan sonra text overlay isteği (F.M.10b). Frontend,
/// SignalR "GenerationUpdate" Completed geldiğinde bu endpoint'i çağırır (stateless
/// post-process — Yaklaşım C). Backend cache'ten pending veriyi çeker, overlay render
/// eder, R2'a yükler, final URL döner.
/// </summary>
public class PresetPostProcessRequest
{
    /// <summary>İlgili generation'ın ID'si (cache anahtarı).</summary>
    public Guid GenerationId { get; set; }

    /// <summary>fal.ai'ın ürettiği ham görsel URL'i (SignalR payload'ından).</summary>
    public string SourceImageUrl { get; set; } = string.Empty;
}

/// <summary>
/// Preset generation başlatılırken IMemoryCache'e konan geçici veri (F.M.10b). Generation
/// tamamlandığında PostProcess bu veriyle text overlay render eder. UserId sahiplik
/// kontrolü için tutulur.
/// </summary>
public class PendingPresetPostProcessing
{
    public string PresetId { get; set; } = string.Empty;
    public Dictionary<string, string> TextValues { get; set; } = new();
    public Guid UserId { get; set; }
}
