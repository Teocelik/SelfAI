using SelfAI.DTOs.Admin;

namespace SelfAI.ViewModels.Admin;

/// <summary>
/// F.9b — Admin model catalog liste ekranı ViewModel'i.
/// </summary>
public class AdminModelsListViewModel
{
    public AdminModelListDto? ModelList { get; set; }
    public string? StatusFilter { get; set; }    // "all", "approved", "pending", "disabled"
    public string? CategoryFilter { get; set; }  // "all", "text-to-image", "text-to-audio", ...
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public List<string> AvailableCategories { get; set; } = new()
    {
        "text-to-image",
        "text-to-audio",
        "text-to-video",
        "image-to-image",
        "image-to-video"
    };

    public List<string> AvailableTiers { get; set; } = new()
    {
        "Fast", "Standard", "Premium", "CharacterLora", "VideoFast", "VideoPremium"
    };
}
