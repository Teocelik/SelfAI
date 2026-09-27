namespace SelfAI.Entities.Enums;

/// <summary>
/// Görsel moderasyon kararı (F.8b Faz A). DB'ye string olarak yazılır
/// (HasConversion&lt;string&gt;) — audit kayıtları migration'sız okunabilir kalsın diye.
/// </summary>
public enum ModerationDecision
{
    /// <summary>Skor soft eşiğin altında — yükleme kabul edilir.</summary>
    Accepted = 0,

    /// <summary>Skor soft eşik ile hard eşik arasında — yükleme reddedilir (sınırda içerik).</summary>
    SoftRejected = 1,

    /// <summary>Skor hard eşiği aşıyor — yükleme reddedilir (net ihlal).</summary>
    HardRejected = 2
}
