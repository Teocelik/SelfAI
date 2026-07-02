using SelfAI.DTOs.Assets;
using SelfAI.Entities.Enums;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Kalıcı asset yönetimi (F.M.7). Storage upload + DB persist + sahiplik kontrollü resolve.
/// Storage detayı IAssetStorageProvider'a delege edilir (SOLID: dependency inversion).
/// </summary>
public interface IAssetService
{
    /// <summary>
    /// IFormFile'ı storage'a yükler, DB'ye Asset kaydı ekler (controller akışları için — validation dahil).
    /// </summary>
    Task<ServiceResult<AssetDto>> UploadAsync(
        IFormFile file,
        Guid userId,
        AssetPurpose purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ham stream'i storage'a yükler, DB'ye Asset kaydı ekler (background task akışları için —
    /// IFormFile request scope'unu aşamadığı durumda byte[]/Stream ile çağrılır).
    /// </summary>
    Task<ServiceResult<AssetDto>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid userId,
        AssetPurpose purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının belirli purpose'daki asset'lerini listeler (silinmemiş).
    /// </summary>
    Task<ServiceResult<List<AssetDto>>> ListUserAssetsAsync(
        Guid userId,
        AssetPurpose? purpose = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asset ID'lerini URL'lere çevirir (orchestrator generation payload'ı için).
    /// Sahiplik kontrolü yapar — userId sahibi değilse hata döner. Sıra korunur.
    /// </summary>
    Task<ServiceResult<List<string>>> ResolveUrlsAsync(
        IEnumerable<Guid> assetIds,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tek asset URL lookup (Face Lock gibi tek dosyalık akışlar için).
    /// </summary>
    Task<ServiceResult<string>> ResolveUrlAsync(
        Guid assetId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asset'i soft delete eder (DeletedAt set). Storage provider'a physical delete
    /// tetiklenir (best effort, hata log'lanır).
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(
        Guid assetId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
