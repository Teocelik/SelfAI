namespace SelfAI.Services.Generation.Abstractions;

/// <summary>
/// Asset storage provider soyutlaması (F.M.7). Domain/servis katmanı fal.ai veya S3
/// concrete'ine bağlanmaz — bu interface üzerinden çalışır. Provider değişimi (fal.ai → S3)
/// yalnızca DI kaydı değişikliği gerektirir.
/// </summary>
public interface IAssetStorageProvider
{
    /// <summary>Provider identifier — "FalAi", "AwsS3", vb.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Dosya stream'ini yükler. Public URL + provider-specific key döner.
    /// </summary>
    Task<AssetUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider'dan dosyayı siler. Başarısız olsa bile logger'a yazar,
    /// exception fırlatmaz (soft delete DB'de zaten yapılmış).
    /// </summary>
    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

/// <summary>Storage upload sonucu — public URL + silme için provider-specific key.</summary>
public class AssetUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
}
