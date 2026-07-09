using System.Text.Json.Serialization;

namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal.ai Flux (dev/schnell) result yanıtının tip-güvenli karşılığı.
/// snake_case alanlar JsonPropertyName ile eşlenir.
/// </summary>
public class FalAiFluxResponse
{
    [JsonPropertyName("images")]
    public List<FalAiImage> Images { get; set; } = new();

    // Seed sadece log/display amaçlı. fal.ai modeline göre integer, decimal veya
    // Int64 sınırını aşan değerler dönebildiği için string olarak tutulur. JSON number
    // de gelebildiğinden FlexibleStringConverter ile token tipi fark etmeksizin parse edilir.
    [JsonPropertyName("seed")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Seed { get; set; }

    [JsonPropertyName("timings")]
    public FalAiTimings? Timings { get; set; }

    [JsonPropertyName("has_nsfw_concepts")]
    public List<bool>? HasNsfwConcepts { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
}

public class FalAiImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }
}

public class FalAiTimings
{
    [JsonPropertyName("inference")]
    public decimal? Inference { get; set; }
}
