using SelfAI.Entities.Enums;

namespace SelfAI.DTOs.Admin;

/// <summary>
/// F.9b — Admin model listesi filtre parametreleri.
/// </summary>
public class AdminModelListFilter
{
    public CatalogStatus? Status { get; set; }
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
