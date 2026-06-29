using SelfAI.Entities.Enums;

namespace SelfAI.Entities;

/// <summary>
/// Studio model picker'ında gösterilen fal.ai modelinin DB kaydı (F.M.5).
/// Curated başlangıç seti seed ile gelir; yeni modeller fal.ai sync'iyle
/// Pending olarak eklenir, admin Approved'a çeker (F.9).
/// </summary>
public class ModelCatalogEntry
{
    public Guid Id { get; set; }

    /// <summary>fal.ai endpoint ID (örn. "fal-ai/flux/dev"). Unique.</summary>
    public string EndpointId { get; set; } = string.Empty;

    /// <summary>UI'de görünen ad (örn. "FLUX.1 [dev]").</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>"text-to-image", "image-to-image", vb.</summary>
    public string Category { get; set; } = "text-to-image";

    /// <summary>Model sağlayıcısı — "Black Forest Labs", "Google", vb.</summary>
    public string? Provider { get; set; }

    public string? ThumbnailUrl { get; set; }

    /// <summary>fal.ai per-output USD maliyet.</summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// ModelTier adı ("Fast"/"Standard"/"Premium"/...). Admin override edebilir;
    /// CharacterLora tier'ı karakter sistemine özel, burada kullanılmaz.
    /// </summary>
    public string Tier { get; set; } = "Standard";

    public CatalogStatus Status { get; set; } = CatalogStatus.Approved;

    /// <summary>Studio'da "Önerilen" rozeti gösterilir.</summary>
    public bool IsRecommended { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
