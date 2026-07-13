using SelfAI.DTOs.Admin;
using SelfAI.DTOs.Catalog;
using SelfAI.Entities.Enums;
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
    /// F.9b — category parametresi eklendi (default "text-to-image", backward compat).
    /// F.M.10c'de "text-to-audio", F.M.9'da "text-to-video" için hazır.
    /// </summary>
    Task<ServiceResult<int>> SyncFromFalAiAsync(
        string category = "text-to-image",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// F.9b — Admin paneli için paginated + filtrelenebilir model listesi (tüm status'ler).
    /// </summary>
    Task<ServiceResult<AdminModelListDto>> GetAdminListAsync(
        AdminModelListFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>F.9b — Tek model detayı (edit formu için).</summary>
    Task<ServiceResult<AdminModelDetailDto>> GetAdminDetailAsync(
        Guid modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// F.9b — Model metadata güncelle (patch pattern). Kategori değişirse ilgili
    /// cache'ler invalidate edilir.
    /// </summary>
    Task<ServiceResult<bool>> UpdateAdminAsync(
        Guid modelId,
        AdminModelUpdateDto update,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// F.9b — Model status değiştir (Approve/Hide). Cache invalidation ile Studio
    /// bir sonraki istekte güncel görür (SignalR event YOK).
    /// </summary>
    Task<ServiceResult<bool>> SetStatusAsync(
        Guid modelId,
        CatalogStatus newStatus,
        CancellationToken cancellationToken = default);
}
