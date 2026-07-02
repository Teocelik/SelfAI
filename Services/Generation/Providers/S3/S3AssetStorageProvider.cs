using SelfAI.Services.Generation.Abstractions;

namespace SelfAI.Services.Generation.Providers.S3;

/// <summary>
/// AWS S3 asset storage provider skeleton. F.7 production hazırlığında aktive edilecek.
/// Şimdilik DI'a KAYIT EDİLMEZ — Program.cs default provider olarak FalAiAssetStorageProvider kullanır.
/// </summary>
public class S3AssetStorageProvider : IAssetStorageProvider
{
    public string ProviderName => "AwsS3";

    public Task<AssetUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "S3 storage F.7 phase'inde implement edilecek. Şu an fal.ai storage kullanılıyor.");
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "S3 storage F.7 phase'inde implement edilecek.");
    }
}
