namespace SelfAI.Services.Generation.Abstractions;

/// <summary>
/// fal.ai storage (asset upload) client (F.M.4 minimal — F.M.7'de Asset entity +
/// S3 migration desteğiyle tam refactor edilecek).
/// </summary>
public interface IFalAiStorageClient
{
    /// <summary>
    /// Dosya stream'ini fal.ai storage'a yükler, public URL döner.
    /// 24 saat default retention.
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
