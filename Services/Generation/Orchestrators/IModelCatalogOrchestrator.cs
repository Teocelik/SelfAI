using SelfAI.DTOs.Catalog;
using SelfAI.Models;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Model catalog use-case koordinatörü (F.M.5). Studio'ya gösterilecek modelleri
/// hazırlar, favori toggle eder, fal.ai'dan yeni modelleri sync eder.
/// Controller yalnızca buna bağlanır (Domain/Infrastructure'a doğrudan değil).
/// </summary>
public interface IModelCatalogOrchestrator
{
    /// <summary>
    /// UI'de gösterilecek modelleri döner. Sadece Approved status'lü modeller.
    /// User ID verilirse favori bilgisi de eklenir.
    /// </summary>
    Task<ServiceResult<CatalogResponseDto>> GetCatalogAsync(
        string category,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının favorilerini günceller (toggle). Dönüş: yeni favori durumu (true=favori).
    /// </summary>
    Task<ServiceResult<bool>> ToggleFavoriteAsync(
        Guid userId,
        string endpointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// fal.ai'dan model listesi çek, yeni keşfedilen modelleri ModelCatalogEntry
    /// tablosuna Pending status ile ekle. Admin panelinde manual tetiklenir (F.9).
    /// Dönüş: eklenen + güncellenen toplam kayıt sayısı.
    /// </summary>
    Task<ServiceResult<int>> SyncFromFalAiAsync(
        CancellationToken cancellationToken = default);
}
