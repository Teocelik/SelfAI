using System.Text.Json.Serialization;

namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal.ai queue submit (POST /{model}) sonrası dönen yanıt.
/// Request henüz tamamlanmamıştır; request_id ile durum sorgulanır.
/// </summary>
public class FalAiQueueSubmitResponse
{
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;  // "IN_QUEUE"

    [JsonPropertyName("response_url")]
    public string? ResponseUrl { get; set; }

    [JsonPropertyName("status_url")]
    public string? StatusUrl { get; set; }

    [JsonPropertyName("cancel_url")]
    public string? CancelUrl { get; set; }
}
