using System.Text.Json.Serialization;

namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal.ai unified model list endpoint (GET api.fal.ai/v1/models) yanıtı (F.M.5).
/// Pagination cursor tabanlı.
/// </summary>
public class FalAiModelListResponse
{
    [JsonPropertyName("models")]
    public List<FalAiModelItem> Models { get; set; } = new();

    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}

public class FalAiModelItem
{
    [JsonPropertyName("endpoint_id")]
    public string EndpointId { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public FalAiModelMetadata? Metadata { get; set; }
}

public class FalAiModelMetadata
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("license_type")]
    public string? LicenseType { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}
