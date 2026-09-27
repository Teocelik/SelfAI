namespace SelfAI.Configurations;

/// <summary>
/// Görsel yükleme moderasyon ayarları (F.8b Faz A). Eşikler business kararıyla değişir,
/// kod değişikliği GEREKTİRMEZ — appsettings "Moderation" section'ından okunur.
/// </summary>
public class ModerationOptions
{
    /// <summary>Configuration section adı — Program.cs bind'inde kullanılır (magic string yerine).</summary>
    public const string SectionName = "Moderation";

    /// <summary>
    /// Net ihlal eşiği. Skor bu değere eşit veya üstündeyse karar HardRejected olur.
    /// </summary>
    public decimal HardThreshold { get; set; } = 0.85m;

    /// <summary>
    /// Sınırda içerik eşiği. Skor bu değer ile <see cref="HardThreshold"/> arasındaysa
    /// karar SoftRejected olur. İki eşik de yüklemeyi reddeder; fark yalnızca
    /// kullanıcıya dönen mesaj ve audit kaydındadır.
    /// </summary>
    public decimal SoftThreshold { get; set; } = 0.60m;

    /// <summary>NSFW olasılığı dönen fal.ai dedektör endpoint'i.</summary>
    public string NsfwModelEndpoint { get; set; } = "fal-ai/imageutils/nsfw";
}
