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
    }
}
