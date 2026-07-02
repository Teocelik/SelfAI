using SelfAI.Entities.Enums;

namespace SelfAI.Entities;

/// <summary>
/// Kalıcı asset kaydı (F.M.7). Tüm upload'lar (Face Lock, Character training, generic)
/// burada persist edilir. Storage provider'dan bağımsız — URL + StorageKey tutulur,
/// böylece fal.ai → S3 geçişi entity değişikliği gerektirmez.
/// </summary>
public class Asset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Storage provider identifier — "FalAi", "AwsS3", "Backblaze", vb.</summary>
    public string StorageProvider { get; set; } = "FalAi";

    /// <summary>Public URL (fal.ai storage veya S3'ten dönen).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Storage provider'a özel dahili referans (S3 key, fal.ai file_id, vb.).
    /// Silme veya replace işlemleri için gerekli.
    /// </summary>
    public string? StorageKey { get; set; }

    public AssetPurpose Purpose { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public long SizeBytes { get; set; }
    public string? OriginalFileName { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Soft delete zaman damgası. null → aktif.</summary>
    public DateTime? DeletedAt { get; set; }
}
