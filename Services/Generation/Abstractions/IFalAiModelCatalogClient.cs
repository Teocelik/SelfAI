using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Abstractions;

/// <summary>
/// fal.ai unified model list endpoint'inden ham model listesini çeken
/// provider-level client (F.M.5). Pagination'ı kendi içinde handle eder,
/// iş kuralı içermez (Infrastructure katmanı).
/// </summary>
public interface IFalAiModelCatalogClient
{
    /// <summary>
    /// fal.ai'dan tüm aktif modelleri çeker. Pagination otomatik handle edilir.
    /// </summary>
    Task<List<FalAiModelItem>> ListAllAsync(
        string? category = null,
        CancellationToken cancellationToken = default);
}
