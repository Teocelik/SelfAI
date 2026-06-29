namespace SelfAI.DTOs.Catalog;

/// <summary>
/// Studio model picker'ında bir kart için gereken veri (F.M.5).
/// CreditCost hesaplanmış kullanıcı kredisidir (fal.ai USD değil).
/// </summary>
public class CatalogModelDto
{
    public string EndpointId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Tier { get; set; } = "Standard";
    public int CreditCost { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsFavorited { get; set; }
}

public class CatalogResponseDto
{
    public List<CatalogModelDto> Models { get; set; } = new();
    public int TotalCount { get; set; }
}
