using SelfAI.Entities.Enums;

namespace SelfAI.DTOs.Moderation;

/// <summary>
/// Görsel moderasyon kontrolünün sonucu (F.8b Faz A). Ham veri taşıyıcı — mantık içermez.
///
/// <see cref="Score"/> yalnızca log/audit içindir; kullanıcıya ASLA gösterilmez
/// (eşik bilgisi sızdırmak filtreyi atlatmayı kolaylaştırır).
/// </summary>
public class ImageModerationResult
{
    /// <summary>Dedektörden dönen NSFW olasılığı (0.0 - 1.0). Log için, kullanıcıya gösterilmez.</summary>
    public decimal Score { get; set; }

    public ModerationDecision Decision { get; set; }

    /// <summary>Reddedilen yüklemede kullanıcıya gösterilecek güvenli mesaj.</summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>Karar kabul mü? Çağıranın eşik karşılaştırması yapmasına gerek kalmaz.</summary>
    public bool IsAccepted => Decision == ModerationDecision.Accepted;
}
