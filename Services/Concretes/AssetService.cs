using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.DTOs.Assets;
using SelfAI.Entities;
using SelfAI.Entities.Enums;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes;

/// <summary>
/// Kalıcı asset servisi (F.M.7). Storage (IAssetStorageProvider) + DB (AppDbContext) koordinasyonu.
/// İş mantığı burada; HttpContext'e erişim YOK (userId dışarıdan parametre gelir).
/// </summary>
public class AssetService : IAssetService
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;  // 10MB
    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/webp" };

    private readonly IAssetStorageProvider _storage;
    private readonly AppDbContext _db;
    private readonly IImageModerationService _imageModeration;
    private readonly IUploadModerationLogService _moderationLog;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        IAssetStorageProvider storage,
        AppDbContext db,
        IImageModerationService imageModeration,
        IUploadModerationLogService moderationLog,
        ILogger<AssetService> logger)
    {
        _storage = storage;
        _db = db;
        _imageModeration = imageModeration;
        _moderationLog = moderationLog;
        _logger = logger;
    }

    public async Task<ServiceResult<AssetDto>> UploadAsync(
        IFormFile file,
        Guid userId,
        AssetPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        // Validation (controller akışı — kullanıcı input)
        if (file == null || file.Length == 0)
            return ServiceResult<AssetDto>.Failure("Dosya boş olamaz.", 400);

        if (file.Length > MaxImageSizeBytes)
            return ServiceResult<AssetDto>.Failure("Dosya boyutu max 10MB olmalı.", 400);

        if (!AllowedContentTypes.Contains(file.ContentType))
            return ServiceResult<AssetDto>.Failure(
                "Sadece JPEG, PNG veya WebP kabul edilir.", 400);

        // F.8b Faz A — moderasyon storage'a YAZMADAN ÖNCE çalışır. Dedektör ham içerik
        // ister (henüz public URL yok), bu yüzden dosya belleğe okunur (max 10MB).
        byte[] bytes;
        using (var buffer = new MemoryStream())
        {
            await file.CopyToAsync(buffer, cancellationToken);
            bytes = buffer.ToArray();
        }

        var moderationResult = await _imageModeration.ModerateAsync(
            bytes, file.ContentType, cancellationToken);

        // Fail-closed: dedektör cevap veremediyse yükleme geçirilmez.
        if (!moderationResult.IsSuccess)
            return ServiceResult<AssetDto>.Failure(
                moderationResult.Message, moderationResult.StatusCode);

        var decision = moderationResult.Data!;

        // KABUL EDİLEN ve REDDEDİLEN her yükleme loglanır.
        await _moderationLog.LogAsync(
            userId, bytes, decision, MapPurposeToSource(purpose), cancellationToken);

        if (!decision.IsAccepted)
            return ServiceResult<AssetDto>.Failure(decision.UserMessage, 422);

        using var stream = new MemoryStream(bytes);
        return await UploadAsync(
            stream, file.FileName, file.ContentType, file.Length, userId, purpose, cancellationToken);
    }

    /// <summary>
    /// Audit kaynağı eşlemesi (F.8b Faz A). Bu overload controller akışlarına hizmet eder;
    /// karakter eğitimi görselleri kendi akışında (request scope'unda, kredi düşümünden önce)
    /// moderasyondan geçtiği için buraya CharacterTraining purpose'u ile gelmez.
    /// </summary>
    private static ModerationSource MapPurposeToSource(AssetPurpose purpose) =>
        purpose == AssetPurpose.CharacterTraining
            ? ModerationSource.CharacterTraining
            : ModerationSource.FaceLock;

    /// <summary>
    /// Ham stream overload'ı (background task akışları). Moderasyon BURADA yapılmaz —
    /// çağıran akış görselleri request scope'unda zaten kontrol etmiştir
    /// (bkz. CharacterTrainingOrchestrator). Aksi halde aynı görsel için ikinci kez
    /// fal.ai dedektör maliyeti çıkardı.
    /// </summary>
    public async Task<ServiceResult<AssetDto>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid userId,
        AssetPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadResult = await _storage.UploadAsync(
                fileStream, fileName, contentType, cancellationToken);

            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StorageProvider = _storage.ProviderName,
                Url = uploadResult.Url,
                StorageKey = uploadResult.StorageKey,
                Purpose = purpose,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                OriginalFileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName,
                CreatedAt = DateTime.UtcNow
            };

            _db.Assets.Add(asset);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Asset yüklendi. | AssetId: {AssetId} | UserId: {UserId} | Purpose: {Purpose} | Provider: {Provider} | Size: {Size}",
                asset.Id, userId, purpose, _storage.ProviderName, sizeBytes);

            return ServiceResult<AssetDto>.Success(MapToDto(asset), "Görsel yüklendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Asset upload hatası. | UserId: {UserId} | Purpose: {Purpose}",
                userId, purpose);
            return ServiceResult<AssetDto>.Failure("Görsel yüklenemedi.", 500);
        }
    }

    public async Task<ServiceResult<List<AssetDto>>> ListUserAssetsAsync(
        Guid userId,
        AssetPurpose? purpose = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Assets
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.DeletedAt == null);

        if (purpose.HasValue)
            query = query.Where(a => a.Purpose == purpose.Value);

        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return ServiceResult<List<AssetDto>>.Success(assets.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<string>>> ResolveUrlsAsync(
        IEnumerable<Guid> assetIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ids = assetIds.ToList();
        if (ids.Count == 0)
            return ServiceResult<List<string>>.Success(new List<string>());

        var assets = await _db.Assets
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id) && a.UserId == userId && a.DeletedAt == null)
            .Select(a => new { a.Id, a.Url })
            .ToListAsync(cancellationToken);

        // Sahiplik + varlık kontrolü
        var foundIds = assets.Select(a => a.Id).ToHashSet();
        var missing = ids.Where(id => !foundIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            _logger.LogWarning(
                "Bazı asset'ler bulunamadı veya kullanıcıya ait değil. | UserId: {UserId} | Missing: {Missing}",
                userId, string.Join(",", missing));
            return ServiceResult<List<string>>.Failure(
                "Bazı görseller bulunamadı veya erişim izniniz yok.", 403);
        }

        // Sıra korunur (orchestrator'da index bazlı ilerleyebilir)
        var urlById = assets.ToDictionary(a => a.Id, a => a.Url);
        var urls = ids.Select(id => urlById[id]).ToList();
        return ServiceResult<List<string>>.Success(urls);
    }

    public async Task<ServiceResult<string>> ResolveUrlAsync(
        Guid assetId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await ResolveUrlsAsync(new[] { assetId }, userId, cancellationToken);
        if (!result.IsSuccess)
            return ServiceResult<string>.Failure(result.Message, result.StatusCode);
        return ServiceResult<string>.Success(result.Data!.First());
    }

    public async Task<ServiceResult<bool>> DeleteAsync(
        Guid assetId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var asset = await _db.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.UserId == userId, cancellationToken);

        if (asset == null)
            return ServiceResult<bool>.Failure("Görsel bulunamadı.", 404);

        if (asset.DeletedAt.HasValue)
            return ServiceResult<bool>.Success(true, "Zaten silinmiş.");

        asset.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        // Storage provider'a physical delete tetikle (best effort — soft delete zaten yapıldı)
        if (!string.IsNullOrEmpty(asset.StorageKey))
        {
            try
            {
                await _storage.DeleteAsync(asset.StorageKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Storage delete hatası (soft delete tamam). | AssetId: {AssetId}",
                    assetId);
            }
        }

        _logger.LogInformation(
            "Asset silindi. | AssetId: {AssetId} | UserId: {UserId}",
            assetId, userId);

        return ServiceResult<bool>.Success(true, "Görsel silindi.");
    }

    private static AssetDto MapToDto(Asset asset) => new()
    {
        Id = asset.Id,
        Url = asset.Url,
        Purpose = asset.Purpose,
        ContentType = asset.ContentType,
        SizeBytes = asset.SizeBytes,
        OriginalFileName = asset.OriginalFileName,
        CreatedAt = asset.CreatedAt
    };
}
