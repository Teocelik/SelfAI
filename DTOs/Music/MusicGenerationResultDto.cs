namespace SelfAI.DTOs.Music;

/// <summary>
/// SignalR "MusicGenerationUpdate" event payload'u (F.M.10c). Üretim tamamlandığında
/// R2 audio URL + albüm kapağı URL'i ile frontend'e push edilir.
/// </summary>
public class MusicGenerationResultDto
{
    public Guid GenerationId { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;

    /// <summary>Kapak üretilemezse boş olabilir — frontend gradient fallback gösterir.</summary>
    public string CoverUrl { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }
    public string CoverAspectRatio { get; set; } = string.Empty;
    public string MusicPrompt { get; set; } = string.Empty;
}
