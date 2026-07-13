namespace SelfAI.DTOs.Admin;

/// <summary>
/// F.9b — Admin model güncelleme (patch pattern). Null alanlar değişmez.
/// </summary>
public class AdminModelUpdateDto
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Provider { get; set; }
    public string? Tier { get; set; }
    public decimal? CostUsd { get; set; }
    public bool? IsRecommended { get; set; }
}
