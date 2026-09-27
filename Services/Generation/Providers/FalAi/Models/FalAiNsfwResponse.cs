using System.Text.Json.Serialization;

namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal-ai/imageutils/nsfw çıktısı (F.8b Faz A). Model tek bir olasılık döner.
/// </summary>
public class FalAiNsfwResponse
{
    /// <summary>Görselin NSFW olma olasılığı (0.0 - 1.0).</summary>
    [JsonPropertyName("nsfw_probability")]
    public decimal NsfwProbability { get; set; }
}
