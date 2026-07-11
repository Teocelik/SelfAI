using SelfAI.DTOs.Admin;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    /// <summary>
    /// F.7.3 — Mini-admin iş mantığı. Kullanıcı arama + kredi ekleme (audit'li).
    /// Tam CRUD/analytics F.9'da gelir.
    /// </summary>
    public interface IAdminService
    {
        /// <summary>
        /// Email ile kullanıcı ara (case-insensitive).
        /// </summary>
        Task<ServiceResult<AdminUserDto>> FindUserByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Id ile kullanıcı getir (AddCredit form'u için).
        /// </summary>
        Task<ServiceResult<AdminUserDto>> FindUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kullanıcıya kredi ekle. TokenWallet.CurrentBalance güncellenir, TokenTransaction
        /// kaydedilir (AdminUserId + AdminNote ile). Tek DB transaction'da atomik.
        /// </summary>
        Task<ServiceResult<AdminGrantResultDto>> AddCreditAsync(
            Guid targetUserId,
            int amount,
            string? note,
            Guid adminUserId,
            CancellationToken cancellationToken = default);

        // ═══ F.9a — Panel genişletme ═══

        /// <summary>
        /// Server-side paginated kullanıcı listesi. Email/isim araması + kayıt tarihi filtresi.
        /// Bakiye/admin/üretim/kredi istatistikleri batch lookup ile doldurulur (N+1 yok).
        /// </summary>
        Task<ServiceResult<AdminUserListDto>> GetUsersPaginatedAsync(
            string? searchTerm,
            DateTime? registeredAfter,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kullanıcı detay bilgisi (bakiye + aggregate istatistikler).
        /// </summary>
        Task<ServiceResult<AdminUserDetailDto>> GetUserDetailAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kullanıcının paginated işlem geçmişi. Admin işlemlerinde admin email batch join ile doldurulur.
        /// </summary>
        Task<ServiceResult<AdminTransactionListDto>> GetUserTransactionsAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dashboard özet metrikleri (bugün/bu hafta kullanıcı, üretim, kredi tüketimi).
        /// </summary>
        Task<ServiceResult<AdminDashboardStatsDto>> GetDashboardStatsAsync(
            CancellationToken cancellationToken = default);
    }
}
