namespace SelfAI.ViewModels.Templates;

/// <summary>
/// Templates sekmesi (/Studio/Templates) view modeli (F.M.10a).
/// </summary>
public class TemplatesIndexViewModel
{
    public List<PostFormatViewModel> Formats { get; set; } = new();
}

/// <summary>
/// Frontend'e taşınan format bilgisi. ModelEndpoint KASITLI olarak yok —
/// backend routing detayı dışarı sızmaz (F.M.10a).
/// </summary>
public class PostFormatViewModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string AspectRatioValue { get; set; } = string.Empty;
    public string PromptSuffix { get; set; } = string.Empty;
}
