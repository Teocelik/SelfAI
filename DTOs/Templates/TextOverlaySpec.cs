namespace SelfAI.DTOs.Templates;

/// <summary>
/// Tek bir metin katmanının render spesifikasyonu (F.M.10b). Preset config'de sabit
/// koordinat/font/renk tanımlıdır; yalnızca <see cref="Text"/> kullanıcıdan gelir.
/// <see cref="ITextOverlayService"/> bu spec listesini ImageSharp ile görsele işler.
/// </summary>
public class TextOverlaySpec
{
    /// <summary>Metin içeriği. Kullanıcı formundan gelir (preset config'de boş).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>X koordinatı (piksel, sol üstten). Preset config'de sabit.</summary>
    public int X { get; set; }

    /// <summary>Y koordinatı (piksel, sol üstten).</summary>
    public int Y { get; set; }

    /// <summary>Text alanının maksimum genişliği. Uzun metin kelime bazlı wrap yapar.</summary>
    public int MaxWidth { get; set; }

    /// <summary>Font boyutu (px).</summary>
    public float FontSize { get; set; } = 48;

    /// <summary>Font ismi: "Inter-Regular", "Inter-Bold", "Inter-SemiBold".</summary>
    public string FontName { get; set; } = "Inter-Bold";

    /// <summary>Metin rengi HEX. Örn: "#FFFFFF"</summary>
    public string Color { get; set; } = "#FFFFFF";

    /// <summary>Hizalama: "left", "center", "right".</summary>
    public string Alignment { get; set; } = "left";

    /// <summary>Gölge (drop shadow) — beyaz metinlerde okunabilirlik için önerilir.</summary>
    public bool DropShadow { get; set; } = true;
}
