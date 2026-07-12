namespace SelfAI.DTOs.Templates;

/// <summary>
/// Hazır şablon (F.M.10b). F.M.10a'nın format-first akışının üstüne EKSTRA: görsel
/// üretimi + sabit layout'lu text overlay birleşimi. Statik katalog
/// (<see cref="Services.Interfaces.IPresetTemplateCatalogService"/>), F.9b'de admin CRUD gelecek.
/// <see cref="ModelEndpoint"/> yalnızca backend routing içindir; karakter seçilirse
/// orchestrator zaten CharacterId'den flux-lora'ya override eder (bu alan override edilmez).
/// </summary>
public class PresetTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>fal.ai endpoint (backend routing). Karakter yolunda orchestrator override eder.</summary>
    public string ModelEndpoint { get; set; } = string.Empty;

    /// <summary>Aspect ratio değeri ("1:1", "9:16", "16:9"). MapAspectRatioToImageSize kullanır.</summary>
    public string AspectRatioValue { get; set; } = string.Empty;

    /// <summary>Kullanıcı prompt'una eklenen kompozisyon suffix'i (preset'e özel, sabit).</summary>
    public string ImagePromptSuffix { get; set; } = string.Empty;

    /// <summary>Preview mockup görseli (/wwwroot/img/presets/ altında). Seçim sırasında görünür.</summary>
    public string PreviewImageUrl { get; set; } = string.Empty;

    /// <summary>Text alanları — kullanıcı doldurur; her alanın koordinatı sabit.</summary>
    public List<PresetTextFieldDto> TextFields { get; set; } = new();
}

/// <summary>
/// Preset içindeki tek text alanı. Kullanıcı <see cref="OverlaySpec"/>.Text dışındaki
/// hiçbir alanı düzenleyemez (koordinat/font/renk sabit).
/// </summary>
public class PresetTextFieldDto
{
    /// <summary>Alan ID'si — form binding için. Örn: "title", "subtitle", "cta".</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Kullanıcıya gösterilen label. Örn: "Başlık".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Placeholder metin.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Maksimum karakter sayısı.</summary>
    public int MaxLength { get; set; } = 100;

    /// <summary>Overlay rendering için sabit spec (koordinat, font, renk).</summary>
    public TextOverlaySpec OverlaySpec { get; set; } = new();
}
