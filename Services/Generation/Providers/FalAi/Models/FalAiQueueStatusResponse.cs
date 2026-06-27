using System.Text.Json.Serialization;

namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal.ai queue status (GET /{model}/requests/{id}/status) sonrası dönen yanıt.
/// Status değerleri case-sensitive: IN_QUEUE | IN_PROGRESS | COMPLETED | FAILED.
/// </summary>
public class FalAiQueueStatusResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonPropertyName("queue_position")]
    public int? QueuePosition { get; set; }

    [JsonPropertyName("logs")]
    public List<FalAiLogEntry>? Logs { get; set; }
}

/// <summary>fal.ai tarafından dönen tekil log kaydı (logs=1 ile gelir).</summary>
public class FalAiLogEntry
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
