namespace SelfAI.ViewModels.Templates;

/// <summary>
/// Templates sekmesi (/Studio/Templates) view modeli (F.M.10a + F.M.10b).
/// </summary>
public class TemplatesIndexViewModel
{
    // F.M.10a — format-first sekmesi
    public List<PostFormatViewModel> Formats { get; set; } = new();

    // F.M.10b — hazır şablon sekmesi
    public List<PresetTemplateViewModel> Presets { get; set; } = new();

    // F.M.10b — kullanıcının Ready karakterleri (her iki sekme için opsiyonel seçim)
    public List<UserCharacterViewModel> UserCharacters { get; set; } = new();
}

/// <summary>
/// Frontend'e taşınan preset bilgisi (F.M.10b). ModelEndpoint KASITLI olarak yok —
/// backend routing dışarı sızmaz (F.M.10a ile aynı disiplin). Text alanları form'da
/// dinamik render edilir; koordinat/font/renk spec'i frontend'e gönderilmez (backend'de kalır).
/// </summary>
public class PresetTemplateViewModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string PreviewImageUrl { get; set; } = string.Empty;
    public List<PresetTextFieldViewModel> TextFields { get; set; } = new();
}

public class PresetTextFieldViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public int MaxLength { get; set; }
}

/// <summary>Karakter seçici için hafif temsil (F.M.10b).</summary>
public class UserCharacterViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
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
