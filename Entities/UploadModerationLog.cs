using SelfAI.Entities.Enums;

namespace SelfAI.Entities;

/// <summary>
/// Görsel yükleme moderasyon audit kaydı (F.8b Faz A). KABUL EDİLEN ve REDDEDİLEN
/// her yükleme için bir satır düşer — sadece red değil.
///
/// Görselin kendisi SAKLANMAZ; yalnızca içerik hash'i tutulur (tekrar yükleme tespiti +
/// ihtilaf durumunda kanıt). Saf entity — method veya iş mantığı içermez.
/// </summary>
public class UploadModerationLog
{
    public Guid Id { get; set; }

    public Guid AppUserId { get; set; }

    /// <summary>Dosya içeriğinin SHA-256 hash'i (64 karakter, lowercase hex).</summary>
    public string ContentHashSha256 { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    /// <summary>Dedektörden dönen NSFW olasılığı (0.0 - 1.0).</summary>
    public decimal NsfwScore { get; set; }

    /// <summary>Accepted / SoftRejected / HardRejected (DB'de string).</summary>
    public ModerationDecision Decision { get; set; }

    /// <summary>FaceLock / CharacterTraining (DB'de string).</summary>
    public ModerationSource Source { get; set; }

    public DateTime CreatedAt { get; set; }
}
