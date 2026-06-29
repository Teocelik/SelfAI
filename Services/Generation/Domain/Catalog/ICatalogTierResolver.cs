using SelfAI.Services.Generation.Pricing;

namespace SelfAI.Services.Generation.Domain.Catalog;

/// <summary>
/// Endpoint ID'sini ModelTier'a çözer (F.M.5). Eski hardcoded dictionary'nin
/// yerini alır — tier artık DB'deki ModelCatalogEntry.Tier'dan okunur.
/// </summary>
public interface ICatalogTierResolver
{
    /// <summary>
    /// Endpoint ID'sini ModelTier'a map'ler. DB'deki ModelCatalogEntry.Tier
    /// field'ından okur. Bilinmeyen endpoint için Standard döner.
    /// </summary>
    Task<ModelTier> ResolveAsync(string endpointId, CancellationToken cancellationToken = default);
}
