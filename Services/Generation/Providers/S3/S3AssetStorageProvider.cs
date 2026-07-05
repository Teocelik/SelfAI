using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Services.Generation.Abstractions;

namespace SelfAI.Services.Generation.Providers.S3;

/// <summary>
/// Cloudflare R2 asset storage provider (F.7.1). S3 API uyumlu; AWSSDK.S3 ile konuşur.
/// Yalnızca Production ortamında DI'a kaydedilir (Program.cs environment-based seçim).
/// Bucket public DEĞİL — indirme için 24 saat TTL presigned GET URL üretilir.
/// </summary>
public class S3AssetStorageProvider : IAssetStorageProvider
{
    public string ProviderName => "CloudflareR2";

    private readonly IAmazonS3 _s3Client;
    private readonly R2Options _options;
    private readonly ILogger<S3AssetStorageProvider> _logger;

    public S3AssetStorageProvider(
        IAmazonS3 s3Client,
        IOptions<R2Options> options,
        ILogger<S3AssetStorageProvider> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AssetUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Key formatı: {yyyy}/{MM}/{Guid}.{ext}
        // Yıl/ay klasörleri manuel liste görüntülemesini kolaylaştırır ve S3 partition
        // performansı için önerilir (aynı prefix altında çok fazla dosya olmasın).
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".bin"
            };
        }

        var now = DateTime.UtcNow;
        var storageKey = $"{now:yyyy}/{now:MM}/{Guid.NewGuid()}{extension}";

        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey,
                InputStream = fileStream,
                ContentType = contentType,
                DisablePayloadSigning = true  // R2 için gerekli — R2 chunked (SigV4 streaming) signature'ı desteklemez.
            };

            await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            _logger.LogInformation(
                "R2 upload başarılı. | Bucket: {Bucket} | Key: {Key} | Size: {Size}",
                _options.BucketName, storageKey, fileStream.Length);

            // Presigned GET URL üret (24 saat TTL — bucket public değil).
            var presignedUrl = GeneratePresignedUrl(storageKey);

            return new AssetUploadResult
            {
                Url = presignedUrl,
                StorageKey = storageKey
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "R2 upload hatası. | Bucket: {Bucket} | Key: {Key}",
                _options.BucketName, storageKey);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

            _logger.LogInformation(
                "R2 delete başarılı. | Bucket: {Bucket} | Key: {Key}",
                _options.BucketName, storageKey);

            return true;
        }
        catch (Exception ex)
        {
            // Soft delete DB'de zaten yapıldı — storage temizliği best-effort, exception fırlatmaz.
            _logger.LogWarning(ex,
                "R2 delete hatası (soft delete DB'de zaten yapıldı). | Key: {Key}",
                storageKey);
            return false;
        }
    }

    /// <summary>
    /// Verilen storage key için presigned GET URL üretir (TTL: R2Options.PresignedUrlExpiryHours).
    /// F.7.1'de sadece UploadAsync sonrası fresh URL üretilip Asset.Url'e yazılır. Süre dolunca URL
    /// kırık olur AMA: Face Lock görseli tekrar seçilirse yeni URL üretilir; character training
    /// görselleri sadece submit anında kullanılır (LoRA URL zaten fal.ai tarafında saklanır).
    /// "Her resolve'da fresh presigned URL" mantığı F.7.2+ scope'unda AssetService'e eklenebilir.
    /// </summary>
    private string GeneratePresignedUrl(string storageKey)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(_options.PresignedUrlExpiryHours),
            Protocol = Protocol.HTTPS
        };

        return _s3Client.GetPreSignedURL(request);
    }
}
