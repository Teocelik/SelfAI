using System.ComponentModel.DataAnnotations;
using SelfAI.Entities.Enums;

namespace SelfAI.ViewModels.Admin;

/// <summary>
/// F.9b — Admin model düzenleme formu ViewModel'i.
/// </summary>
public class AdminModelEditViewModel
{
    public Guid Id { get; set; }
    public string EndpointId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Display Name gerekli.")]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Kategori gerekli.")]
    public string Category { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Provider { get; set; }

    [Required(ErrorMessage = "Tier gerekli.")]
    public string Tier { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "CostUsd 0-100 arası olmalı.")]
    public decimal CostUsd { get; set; }

    public bool IsRecommended { get; set; }

    public CatalogStatus Status { get; set; }

    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<string> AvailableCategories { get; set; } = new();
    public List<string> AvailableTiers { get; set; } = new();
}
