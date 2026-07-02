using SelfAI.Services.Generation.Abstractions;

namespace SelfAI.Services.Generation.Providers.FalAi;

/// <summary>
/// fal.ai storage üzerinden asset storage provider (F.M.7). Mevcut IFalAiStorageClient'ı
/// wrap eder; IAssetStorageProvider kontratını doldurur. F.7'de S3AssetStorageProvider'a geçilir.
/// </summary>
public class FalAiAssetStorageProvider : IAssetStorageProvider
{
    public string ProviderName => "FalAi";

    private readonly IFalAiStorageClient _falAiStorage;
    private readonly ILogger<FalAiAssetStorageProvider> _logger;

    public FalAiAssetStorageProvider(
        IFalAiStorageClient falAiStorage,
        ILogger<FalAiAssetStorageProvider> logger)
    {
        _falAiStorage = falAiStorage;
        _logger = logger;
    }

    public async Task<AssetUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var url = await _falAiStorage.UploadAsync(fileStream, fileName, contentType, cancellationToken);

        // fal.ai storage key = URL'in dosya path'i (silme için referans).
        // Örn: https://v3b.fal.media/files/b/0a9ffccc/TznZY3ipWk664YquQxw-p.jpg
        //      → StorageKey: "0a9ffccc/TznZY3ipWk664YquQxw-p.jpg"
        string? storageKey = null;
        try
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.TrimStart('/').Split('/', 3);
            if (pathParts.Length >= 3) storageKey = pathParts[2];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "fal.ai storage key extract edilemedi. | Url: {Url}", url);
        }

        return new AssetUploadResult { Url = url, StorageKey = storageKey };
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        // fal.ai storage'da delete endpoint doküman edilmemiş / mevcut değil.
        // Şimdilik no-op — 24 saat sonra otomatik silinir.
        _logger.LogInformation(
            "fal.ai storage delete no-op (24sa retention). | StorageKey: {Key}",
            storageKey);
        return Task.FromResult(true);
    }
}
