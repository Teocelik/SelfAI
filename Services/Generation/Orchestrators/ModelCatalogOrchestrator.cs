using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SelfAI.Data;
using SelfAI.DTOs.Catalog;
using SelfAI.Entities;
using SelfAI.Entities.Enums;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Pricing;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Model catalog orchestrator (F.M.5). DB'den Approved modelleri okur, credit cost
/// hesaplar, IMemoryCache'te (15 dk sliding) tutar; favori toggle ve fal.ai sync yönetir.
///
/// Cache yalnızca base catalog'u (favori bilgisi olmadan) tutar; favori flag'leri
/// her istekte kullanıcıya özel katmanlanır — paylaşılan cache nesnesi mutasyona uğramaz.
/// </summary>
public class ModelCatalogOrchestrator : IModelCatalogOrchestrator
{
    private const int CacheTtlMinutes = 15;

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ICreditPricingService _pricingService;
    private readonly IFalAiModelCatalogClient _catalogClient;
    private readonly ILogger<ModelCatalogOrchestrator> _logger;

    public ModelCatalogOrchestrator(
        AppDbContext db,
        IMemoryCache cache,
        ICreditPricingService pricingService,
        IFalAiModelCatalogClient catalogClient,
        ILogger<ModelCatalogOrchestrator> logger)
    {
        _db = db;
        _cache = cache;
        _pricingService = pricingService;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public async Task<ServiceResult<CatalogResponseDto>> GetCatalogAsync(
        string category,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return ServiceResult<CatalogResponseDto>.Failure("Kategori belirtilmedi.", 400);

        var baseModels = await GetBaseCatalogAsync(category, cancellationToken);

        // Favori flag'leri kullanıcıya özel — cache'lenmiş base nesneler kopyalanır.
        var favoriteEndpoints = new HashSet<string>();
        if (userId.HasValue)
        {
            favoriteEndpoints = (await _db.UserFavoriteModels
                .AsNoTracking()
                .Where(f => f.UserId == userId.Value)
                .Select(f => f.EndpointId)
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        var models = baseModels.Select(m => new CatalogModelDto
        {
            EndpointId = m.EndpointId,
            DisplayName = m.DisplayName,
            Description = m.Description,
            Category = m.Category,
            Provider = m.Provider,
            ThumbnailUrl = m.ThumbnailUrl,
            Tier = m.Tier,
            CreditCost = m.CreditCost,
            IsRecommended = m.IsRecommended,
            IsFavorited = favoriteEndpoints.Contains(m.EndpointId)
        }).ToList();

        return ServiceResult<CatalogResponseDto>.Success(new CatalogResponseDto
        {
            Models = models,
            TotalCount = models.Count
        });
    }

    public async Task<ServiceResult<bool>> ToggleFavoriteAsync(
        Guid userId,
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
            return ServiceResult<bool>.Failure("Endpoint ID gerekli.", 400);

        var existing = await _db.UserFavoriteModels
            .FirstOrDefaultAsync(f => f.UserId == userId && f.EndpointId == endpointId, cancellationToken);

        bool isFavoritedNow;
        if (existing != null)
        {
            _db.UserFavoriteModels.Remove(existing);
            isFavoritedNow = false;
        }
        else
        {
            _db.UserFavoriteModels.Add(new UserFavoriteModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EndpointId = endpointId,
                CreatedAt = DateTime.UtcNow
            });
            isFavoritedNow = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Favori güncellendi. | UserId: {UserId} | Endpoint: {Endpoint} | Favorited: {Fav}",
            userId, endpointId, isFavoritedNow);

        return ServiceResult<bool>.Success(isFavoritedNow);
    }

    public async Task<ServiceResult<int>> SyncFromFalAiAsync(
        CancellationToken cancellationToken = default)
    {
        const string category = "text-to-image";
        var items = await _catalogClient.ListAllAsync(category, cancellationToken);

        if (items.Count == 0)
        {
            _logger.LogWarning("fal.ai sync: hiç model dönmedi (endpoint/şema sorunu olabilir).");
            return ServiceResult<int>.Success(0, "fal.ai'dan model alınamadı.");
        }

        var existing = await _db.ModelCatalogEntries.ToDictionaryAsync(e => e.EndpointId, cancellationToken);
        int added = 0, updated = 0;
        var now = DateTime.UtcNow;

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.EndpointId))
                continue;

            var md = item.Metadata;

            if (existing.TryGetValue(item.EndpointId, out var entry))
            {
                // Metadata güncelle; Status/Tier/CostUsd'a DOKUNMA (admin override koruması).
                if (!string.IsNullOrWhiteSpace(md?.DisplayName)) entry.DisplayName = md.DisplayName;
                if (!string.IsNullOrWhiteSpace(md?.Description)) entry.Description = md.Description;
                if (!string.IsNullOrWhiteSpace(md?.ThumbnailUrl)) entry.ThumbnailUrl = md.ThumbnailUrl;
                entry.UpdatedAt = now;
                updated++;
            }
            else
            {
                _db.ModelCatalogEntries.Add(new ModelCatalogEntry
                {
                    Id = Guid.NewGuid(),
                    EndpointId = item.EndpointId,
                    DisplayName = string.IsNullOrWhiteSpace(md?.DisplayName) ? item.EndpointId : md.DisplayName,
                    Description = md?.Description,
                    Category = string.IsNullOrWhiteSpace(md?.Category) ? category : md.Category,
                    ThumbnailUrl = md?.ThumbnailUrl,
                    CostUsd = 0m,            // Bilinmiyor — admin Approved'a çekerken set eder
                    Tier = "Standard",
                    Status = CatalogStatus.Pending,
                    IsRecommended = false,
                    CreatedAt = now
                });
                added++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        InvalidateCache(category);

        _logger.LogInformation(
            "fal.ai catalog sync tamamlandı. | Added: {Added} | Updated: {Updated}",
            added, updated);

        return ServiceResult<int>.Success(added + updated,
            $"{added} yeni, {updated} güncellenen model.");
    }

    /// <summary>
    /// Approved modellerin base listesini (favori bilgisi olmadan) cache'li döner.
    /// Cache key formatı: "catalog:{category}:approved", 15 dk sliding expiration.
    /// </summary>
    private async Task<List<CatalogModelDto>> GetBaseCatalogAsync(
        string category, CancellationToken cancellationToken)
    {
        var cacheKey = $"catalog:{category}:approved";
        if (_cache.TryGetValue(cacheKey, out List<CatalogModelDto>? cached) && cached != null)
            return cached;

        var entries = await _db.ModelCatalogEntries
            .AsNoTracking()
            .Where(e => e.Status == CatalogStatus.Approved && e.Category == category)
            .OrderByDescending(e => e.IsRecommended)
            .ThenBy(e => e.CostUsd)
            .ToListAsync(cancellationToken);

        var models = entries.Select(e => new CatalogModelDto
        {
            EndpointId = e.EndpointId,
            DisplayName = e.DisplayName,
            Description = e.Description,
            Category = e.Category,
            Provider = e.Provider,
            ThumbnailUrl = e.ThumbnailUrl,
            Tier = e.Tier,
            CreditCost = _pricingService.CalculateUserCredits(e.CostUsd, ParseTier(e.Tier)),
            IsRecommended = e.IsRecommended,
            IsFavorited = false
        }).ToList();

        _cache.Set(cacheKey, models, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(CacheTtlMinutes)
        });

        return models;
    }

    private void InvalidateCache(string category)
        => _cache.Remove($"catalog:{category}:approved");

    /// <summary>
    /// Catalog string tier → ModelTier (bulk credit hesabı için, DB lookup'sız).
    /// Bilinmeyen değer Standard'a düşer.
    /// </summary>
    private static ModelTier ParseTier(string tier) => tier switch
    {
        "Fast" => ModelTier.Fast,
        "Standard" => ModelTier.Standard,
        "Premium" => ModelTier.Premium,
        "CharacterLora" => ModelTier.CharacterLora,
        "VideoFast" => ModelTier.VideoFast,
        "VideoPremium" => ModelTier.VideoPremium,
        _ => ModelTier.Standard
    };
}
