using SelfAI.Entities.Enums;

namespace SelfAI.DTOs.Admin;

/// <summary>
/// F.9b — Admin model detay projection'ı (edit formu için).
/// </summary>
public class AdminModelDetailDto
{
    public Guid Id { get; set; }
    public string EndpointId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
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
