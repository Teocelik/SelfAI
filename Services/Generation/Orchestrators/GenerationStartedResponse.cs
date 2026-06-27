namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Generation başlatıldı yanıtı (fire-and-forget). Image URL'leri YOK —
/// sonuç SignalR "GenerationUpdate" event'i ile gelir.
/// </summary>
public class GenerationStartedResponse
{
    public Guid GenerationId { get; set; }  // Bizim tracking ID'miz (history string key olarak da kullanılır)
    public string Status { get; set; } = "Processing";
    public int CreditsCharged { get; set; }
}
