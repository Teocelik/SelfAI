using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SelfAI.Data;
using SelfAI.DTOs.Admin;
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
        string category = "text-to-image",
        CancellationToken cancellationToken = default)
    {
        var items = await _catalogClient.ListAllAsync(category, cancellationToken);

        if (items.Count == 0)
        {
            _logger.LogWarning(
                "fal.ai sync: hiç model dönmedi (endpoint/şema sorunu olabilir). | Category: {Category}",
                category);
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

    // ── F.9b — Admin catalog yönetimi ─────────────────────────────────────────

    public async Task<ServiceResult<AdminModelListDto>> GetAdminListAsync(
        AdminModelListFilter filter,
        CancellationToken cancellationToken = default)
    {
        // Validation / clamp
        if (filter.Page < 1) filter.Page = 1;
        if (filter.PageSize < 10) filter.PageSize = 20;
        if (filter.PageSize > 100) filter.PageSize = 100;

        var query = _db.ModelCatalogEntries.AsNoTracking().AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(m => m.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(m => m.Category == filter.Category);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(m =>
                m.EndpointId.ToLower().Contains(term) ||
                m.DisplayName.ToLower().Contains(term) ||
                (m.Provider != null && m.Provider.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.Status)                 // Approved(0) → Pending(1) → Hidden(2) → Deprecated(3)
            .ThenByDescending(m => m.IsRecommended)
            .ThenBy(m => m.CostUsd)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(m => new AdminModelListItemDto
            {
                Id = m.Id,
                EndpointId = m.EndpointId,
                DisplayName = m.DisplayName,
                Category = m.Category,
                Provider = m.Provider,
                ThumbnailUrl = m.ThumbnailUrl,
                Tier = m.Tier,
                CostUsd = m.CostUsd,
                Status = m.Status,
                IsRecommended = m.IsRecommended,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<AdminModelListDto>.Success(new AdminModelListDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<ServiceResult<AdminModelDetailDto>> GetAdminDetailAsync(
        Guid modelId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.ModelCatalogEntries.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);

        if (entry == null)
            return ServiceResult<AdminModelDetailDto>.Failure("Model bulunamadı.", 404);

        return ServiceResult<AdminModelDetailDto>.Success(new AdminModelDetailDto
        {
            Id = entry.Id,
            EndpointId = entry.EndpointId,
            DisplayName = entry.DisplayName,
            Description = entry.Description,
            Category = entry.Category,
            Provider = entry.Provider,
            ThumbnailUrl = entry.ThumbnailUrl,
            Tier = entry.Tier,
            CostUsd = entry.CostUsd,
            Status = entry.Status,
            IsRecommended = entry.IsRecommended,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        });
    }

    public async Task<ServiceResult<bool>> UpdateAdminAsync(
        Guid modelId,
        AdminModelUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.ModelCatalogEntries
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);

        if (entry == null)
            return ServiceResult<bool>.Failure("Model bulunamadı.", 404);

        var oldCategory = entry.Category;

        // Patch pattern — null alanlar değişmez.
        if (!string.IsNullOrWhiteSpace(update.DisplayName))
            entry.DisplayName = update.DisplayName.Trim();

        if (update.Description != null) // boş string ile temizleme desteklenir
            entry.Description = string.IsNullOrWhiteSpace(update.Description) ? null : update.Description.Trim();

        if (!string.IsNullOrWhiteSpace(update.Category))
            entry.Category = update.Category.Trim();

        if (update.Provider != null)
            entry.Provider = string.IsNullOrWhiteSpace(update.Provider) ? null : update.Provider.Trim();

        if (!string.IsNullOrWhiteSpace(update.Tier))
        {
            // Tier whitelist — ParseTier'ın bildiği değerler.
            var validTiers = new[] { "Fast", "Standard", "Premium", "CharacterLora",
                                     "VideoFast", "VideoPremium" };
            if (!validTiers.Contains(update.Tier))
                return ServiceResult<bool>.Failure(
                    $"Geçersiz tier: {update.Tier}. Geçerli değerler: {string.Join(", ", validTiers)}",
                    400);
            entry.Tier = update.Tier;
        }

        if (update.CostUsd.HasValue)
        {
            if (update.CostUsd.Value < 0)
                return ServiceResult<bool>.Failure("CostUsd negatif olamaz.", 400);
            entry.CostUsd = update.CostUsd.Value;
        }

        if (update.IsRecommended.HasValue)
            entry.IsRecommended = update.IsRecommended.Value;

        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        // Cache invalidation — hem eski hem yeni kategori (kategori değiştiyse).
        InvalidateCache(oldCategory);
        if (entry.Category != oldCategory)
            InvalidateCache(entry.Category);

        _logger.LogInformation(
            "Model catalog güncellendi. | ModelId: {Id} | Endpoint: {Endpoint} | Category: {OldCat}→{NewCat}",
            modelId, entry.EndpointId, oldCategory, entry.Category);

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> SetStatusAsync(
        Guid modelId,
        CatalogStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.ModelCatalogEntries
            .FirstOrDefaultAsync(m => m.Id == modelId, cancellationToken);

        if (entry == null)
            return ServiceResult<bool>.Failure("Model bulunamadı.", 404);

        var oldStatus = entry.Status;
        entry.Status = newStatus;
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        InvalidateCache(entry.Category);

        _logger.LogInformation(
            "Model status değiştirildi. | ModelId: {Id} | Endpoint: {Endpoint} | Status: {Old}→{New}",
            modelId, entry.EndpointId, oldStatus, newStatus);

        return ServiceResult<bool>.Success(true);
    }

    // ──────────────────────────────────────────────────────────────────────────

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
