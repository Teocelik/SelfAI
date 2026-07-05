namespace SelfAI.Configurations;

/// <summary>
/// Cloudflare R2 storage konfigürasyonu (F.7.1). S3 API uyumlu, egress ücretsiz.
/// Credentials User Secrets'ta ("R2:AccessKeyId", "R2:SecretAccessKey"),
/// production sunucusunda environment variable ile bind edilir. appsettings.Production.json'a
/// gizli değerler YAZILMAZ. Yalnızca Production ortamında S3AssetStorageProvider tarafından kullanılır.
/// </summary>
public class R2Options
{
    /// <summary>Configuration section adı — Program.cs bind'inde kullanılır.</summary>
    public const string SectionName = "R2";

    /// <summary>R2 access key id (S3 uyumlu credential).</summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>R2 secret access key (S3 uyumlu credential).</summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// R2 endpoint URL. Örn: https://{accountId}.r2.cloudflarestorage.com
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Yüklemelerin yapılacağı R2 bucket adı.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Presigned URL geçerlilik süresi (saat cinsinden).
    /// Varsayılan 24 saat — fal.ai upload sonrası hemen indirir, bol süre. Kullanıcı Face Lock
    /// görselini tekrar üretimde kullanırsa yeni URL oluşturulur (F.7.2+ scope).
    /// </summary>
    public int PresignedUrlExpiryHours { get; set; } = 24;
}
