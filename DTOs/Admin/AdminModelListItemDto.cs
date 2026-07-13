using SelfAI.Entities.Enums;

namespace SelfAI.DTOs.Admin;

/// <summary>
/// F.9b — Admin model catalog liste satırı. Read-only projection.
/// </summary>
public class AdminModelListItemDto
{
    public Guid Id { get; set; }
    public string EndpointId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Tier { get; set; } = string.Empty;
    public decimal CostUsd { get; set; }
    public CatalogStatus Status { get; set; }
    public bool IsRecommended { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
