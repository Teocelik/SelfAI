using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.Services.Generation.Pricing;

namespace SelfAI.Services.Generation.Domain.Catalog;

/// <summary>
/// DB-driven tier resolver (F.M.5). ModelCatalogEntry.Tier string'ini
/// ModelTier enum'una çevirir. Bilinmeyen endpoint/tier için Standard fallback.
/// </summary>
public class CatalogTierResolver : ICatalogTierResolver
{
    private readonly AppDbContext _db;
    private readonly ILogger<CatalogTierResolver> _logger;

    public CatalogTierResolver(AppDbContext db, ILogger<CatalogTierResolver> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ModelTier> ResolveAsync(
        string endpointId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
            return ModelTier.Standard;

        var tier = await _db.ModelCatalogEntries
            .AsNoTracking()
            .Where(e => e.EndpointId == endpointId)
            .Select(e => e.Tier)
            .FirstOrDefaultAsync(cancellationToken);

        if (tier == null)
        {
            _logger.LogWarning(
                "Bilinmeyen endpoint için tier resolve denendi, Standard varsayılır. | Endpoint: {Endpoint}",
                endpointId);
            return ModelTier.Standard;
        }

        return tier switch
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
}
