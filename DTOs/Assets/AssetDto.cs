using SelfAI.Entities.Enums;

namespace SelfAI.DTOs.Assets;

/// <summary>Frontend'e dönen asset temsili (F.M.7). Upload sonucu + listeleme.</summary>
public class AssetDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public AssetPurpose Purpose { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? OriginalFileName { get; set; }
    public DateTime CreatedAt { get; set; }
}
