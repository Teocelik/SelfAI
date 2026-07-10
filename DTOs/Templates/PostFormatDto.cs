namespace SelfAI.DTOs.Templates;

/// <summary>
/// Sosyal medya post formatı (F.M.10a). Statik katalog — kod içi tanımlı,
/// F.9'da CRUD'lu admin yönetimi gelir. Frontend'e ViewModel üzerinden yansır;
/// <see cref="ModelEndpoint"/> yalnızca backend routing içindir, dışarı sızmaz.
/// </summary>
public class PostFormatDto
{
    /// <summary>Unique format identifier (kebab-case). Örn: "instagram-post"</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Kullanıcıya gösterilecek isim. Örn: "Instagram Post"</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Kısa açıklama. Örn: "1080×1080, kare format, Instagram feed için"</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Platform ikonu/rengi için semantic key. Örn: "instagram", "youtube", "tiktok"</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Görsel genişliği (piksel). Kullanıcıya gösterilir + sonuç kartı boyutu.</summary>
    public int Width { get; set; }

    /// <summary>Görsel yüksekliği (piksel).</summary>
    public int Height { get; set; }

    /// <summary>
    /// Kullanılan fal.ai endpoint (Ideogram V3 veya Nano Banana). SADECE backend
    /// routing — frontend'e gönderilmez (ViewModel bu alanı taşımaz).
    /// </summary>
    public string ModelEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Aspect ratio değeri. Örn: "1:1", "9:16", "16:9". Mevcut generation pipeline'ı
    /// (StartGenerationRequest.AspectRatio) bunu image_size enum'una / aspect_ratio'ya çevirir.
    /// </summary>
    public string AspectRatioValue { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcının prompt'una eklenecek kompozisyon önerisi suffix. Kullanıcı görebilir
    /// ve isterse kapatabilir (transparent — F.M.10a kararı).
    /// </summary>
    public string PromptSuffix { get; set; } = string.Empty;
}
