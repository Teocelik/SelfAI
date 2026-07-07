using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Data;
using SelfAI.DTOs.Admin;
using SelfAI.Entities;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    /// <summary>
    /// F.7.3 — Mini-admin servisi. Kullanıcı arama + kredi ekleme.
    ///
    /// Yetki modeli: ASP.NET Core Identity KULLANILMAZ (Firebase Auth). "IsAdmin" bilgisi
    /// AdminOptions.AllowedEmails üzerinden hesaplanır (UserManager/RoleManager yok).
    /// Cüzdan mutasyonu CreditService kalıbıyla aynı (CurrentBalance + UpdatedAt), ek olarak
    /// TokenTransaction.AdminUserId + AdminNote audit alanları yazılır.
    /// </summary>
    public class AdminService : IAdminService
    {
        private const int MinAmount = 1;
        private const int MaxAmount = 10000;

        private readonly AppDbContext _db;
        private readonly AdminOptions _adminOptions;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            AppDbContext db,
            IOptions<AdminOptions> adminOptions,
            ILogger<AdminService> logger)
        {
            _db = db;
            _adminOptions = adminOptions.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<AdminUserDto>> FindUserByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<AdminUserDto>.Failure("Email boş olamaz.", 400);

            var normalized = email.Trim().ToLower();

            // Proje NormalizedEmail tutmuyor — SQL LOWER() ile case-insensitive arama.
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);

            if (user == null)
                return ServiceResult<AdminUserDto>.Failure("Bu email ile kullanıcı bulunamadı.", 404);

            return ServiceResult<AdminUserDto>.Success(await BuildUserDtoAsync(user, cancellationToken));
        }

        public async Task<ServiceResult<AdminUserDto>> FindUserByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                return ServiceResult<AdminUserDto>.Failure("Kullanıcı bulunamadı.", 404);

            return ServiceResult<AdminUserDto>.Success(await BuildUserDtoAsync(user, cancellationToken));
        }

        public async Task<ServiceResult<AdminGrantResultDto>> AddCreditAsync(
            Guid targetUserId,
            int amount,
            string? note,
            Guid adminUserId,
            CancellationToken cancellationToken = default)
        {
            // Server-side validation (ViewModel DataAnnotations'a ek güvenlik katmanı).
            if (amount < MinAmount)
                return ServiceResult<AdminGrantResultDto>.Failure($"Miktar en az {MinAmount} olmalı.", 400);

            if (amount > MaxAmount)
                return ServiceResult<AdminGrantResultDto>.Failure($"Miktar en fazla {MaxAmount} olabilir.", 400);

            if (!string.IsNullOrWhiteSpace(note) && note.Trim().Length > 500)
                return ServiceResult<AdminGrantResultDto>.Failure("Not en fazla 500 karakter olabilir.", 400);

            // Atomik güncelleme: wallet mutasyonu + audit transaction kaydı aynı DB transaction'da.
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
                if (user == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return ServiceResult<AdminGrantResultDto>.Failure("Kullanıcı bulunamadı.", 404);
                }

                var wallet = await _db.TokenWallets
                    .FirstOrDefaultAsync(w => w.UserId == targetUserId, cancellationToken);

                // Edge case: cüzdan yoksa oluştur (normalde her kullanıcıda vardır).
                if (wallet == null)
                {
                    wallet = new TokenWallet
                    {
                        UserId = targetUserId,
                        CurrentBalance = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.TokenWallets.Add(wallet);
                }

                var previousBalance = wallet.CurrentBalance;
                wallet.CurrentBalance += amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

                var tx = new TokenTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = targetUserId,
                    Amount = amount,
                    Type = TokenTransactionType.ManualAdjust,   // "Admin tarafından elle düzeltme"
                    Description = "Admin kredi ekleme",
                    AdminUserId = adminUserId,
                    AdminNote = trimmedNote,
                    CreatedAt = DateTime.UtcNow
                };
                _db.TokenTransactions.Add(tx);

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Admin kredi ekleme başarılı. | AdminId: {AdminId} | TargetUserId: {TargetUserId} | " +
                    "Amount: {Amount} | PreviousBalance: {PrevBalance} | NewBalance: {NewBalance}",
                    adminUserId, targetUserId, amount, previousBalance, wallet.CurrentBalance);

                return ServiceResult<AdminGrantResultDto>.Success(new AdminGrantResultDto
                {
                    UserId = targetUserId,
                    Email = user.Email ?? string.Empty,
                    PreviousBalance = previousBalance,
                    NewBalance = wallet.CurrentBalance,
                    Amount = amount,
                    Note = trimmedNote,
                    Timestamp = tx.CreatedAt
                }, $"{amount} kredi başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex,
                    "Admin kredi ekleme hatası. | AdminId: {AdminId} | TargetUserId: {TargetUserId}",
                    adminUserId, targetUserId);
                return ServiceResult<AdminGrantResultDto>.Failure("Kredi eklenirken hata oluştu.", 500);
            }
        }

        /// <summary>
        /// AppUser + cüzdan bakiyesi + admin durumundan AdminUserDto kurar.
        /// </summary>
        private async Task<AdminUserDto> BuildUserDtoAsync(AppUser user, CancellationToken ct)
        {
            var balance = await _db.TokenWallets
                .AsNoTracking()
                .Where(w => w.UserId == user.Id)
                .Select(w => (int?)w.CurrentBalance)
                .FirstOrDefaultAsync(ct);

            var isAdmin = !string.IsNullOrWhiteSpace(user.Email)
                && _adminOptions.AllowedEmails.Any(a =>
                    string.Equals(a?.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase));

            return new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                CurrentBalance = balance ?? 0,
                CreatedAt = user.CreatedAt,
                IsAdmin = isAdmin
            };
        }
    }
}
