namespace SelfAI.Services.Generation.Domain.Image;

/// <summary>
/// Domain-level generation sonucu. fal.ai provider DTO'sundan map'lenir,
/// orchestrator buradan SignalR payload + history media kayıtlarını üretir.
/// </summary>
public class ImageGenerationResult
{
    public List<GeneratedImage> Images { get; set; } = new();
    public long? Seed { get; set; }
    public decimal? InferenceTimeSeconds { get; set; }
}

public class GeneratedImage
{
    public string Url { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? ContentType { get; set; }
}
