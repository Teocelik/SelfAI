namespace SelfAI.DTOs.Music;

/// <summary>
/// Üretim başlatıldı yanıtı (F.M.10c). Sonuç senkron dönmez; SignalR
/// "MusicGenerationUpdate" event'iyle gelir.
/// </summary>
public class MusicGenerationStartedDto
{
    public Guid GenerationId { get; set; }
    public string Status { get; set; } = "Started";
    public string Mode { get; set; } = string.Empty;
    public int CreditsCharged { get; set; }
    public string Message { get; set; } = string.Empty;
}
