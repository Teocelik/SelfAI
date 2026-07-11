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

        private const int MinPageSize = 10;
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 20;

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

        // ═══════════════════════════════════════════════════════════════════════
        // F.9a — Panel genişletme
        // ═══════════════════════════════════════════════════════════════════════

        public async Task<ServiceResult<AdminUserListDto>> GetUsersPaginatedAsync(
            string? searchTerm,
            DateTime? registeredAfter,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < MinPageSize) pageSize = DefaultPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            // NOT: AppUser'da soft-delete (DeletedAt) alanı YOK → filtre uygulanmaz.
            var query = _db.Users.AsNoTracking().AsQueryable();

            // Arama: email VEYA isim (proje NormalizedEmail tutmuyor → LOWER karşılaştırma).
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(normalized) ||
                    (u.DisplayName != null && u.DisplayName.ToLower().Contains(normalized)));
            }

            if (registeredAfter.HasValue)
                query = query.Where(u => u.CreatedAt >= registeredAfter.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.DisplayName,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .ToListAsync(cancellationToken);

            var userIds = users.Select(u => u.Id).ToList();

            // Bakiye — batch (N+1 önle).
            var wallets = await _db.TokenWallets.AsNoTracking()
                .Where(w => userIds.Contains(w.UserId))
                .Select(w => new { w.UserId, w.CurrentBalance })
                .ToDictionaryAsync(w => w.UserId, w => w.CurrentBalance, cancellationToken);

            // Üretim sayısı — batch (Generations tablosu, semantik doğru kaynak).
            var genStats = await _db.Generations.AsNoTracking()
                .Where(g => userIds.Contains(g.UserId))
                .GroupBy(g => g.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count, cancellationToken);

            // Kullanılan kredi — batch (negatif transaction toplamı, mutlak).
            var creditStats = await _db.TokenTransactions.AsNoTracking()
                .Where(t => userIds.Contains(t.UserId) && t.Amount < 0)
                .GroupBy(t => t.UserId)
                .Select(g => new { UserId = g.Key, Used = g.Sum(t => -t.Amount) })
                .ToDictionaryAsync(g => g.UserId, g => g.Used, cancellationToken);

            // Admin durumu — Identity YOK, AdminOptions.AllowedEmails üzerinden hesaplanır.
            var adminSet = BuildAdminEmailSet();

            var items = users.Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                DisplayName = u.DisplayName,
                CurrentBalance = wallets.TryGetValue(u.Id, out var bal) ? bal : 0,
                CreatedAt = u.CreatedAt,
                IsAdmin = IsAdminEmail(u.Email, adminSet),
                LastLoginAt = u.LastLoginAt,
                TotalGenerations = genStats.TryGetValue(u.Id, out var gc) ? gc : 0,
                TotalCreditsUsed = creditStats.TryGetValue(u.Id, out var cu) ? cu : 0
            }).ToList();

            return ServiceResult<AdminUserListDto>.Success(new AdminUserListDto
            {
                Users = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<AdminUserDetailDto>> GetUserDetailAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return ServiceResult<AdminUserDetailDto>.Failure("Kullanıcı bulunamadı.", 404);

            var balance = await _db.TokenWallets.AsNoTracking()
                .Where(w => w.UserId == userId)
                .Select(w => (int?)w.CurrentBalance)
                .FirstOrDefaultAsync(cancellationToken);

            var totalGenerations = await _db.Generations.AsNoTracking()
                .CountAsync(g => g.UserId == userId, cancellationToken);

            var totalCreditsUsed = await _db.TokenTransactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.Amount < 0)
                .SumAsync(t => -t.Amount, cancellationToken);

            var totalCreditsGranted = await _db.TokenTransactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.Amount > 0 && t.AdminUserId != null)
                .SumAsync(t => t.Amount, cancellationToken);

            var adminSet = BuildAdminEmailSet();

            return ServiceResult<AdminUserDetailDto>.Success(new AdminUserDetailDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                CurrentBalance = balance ?? 0,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsAdmin = IsAdminEmail(user.Email, adminSet),
                IsDeleted = false,   // AppUser'da soft-delete alanı yok (F.9b'de gelebilir)
                TotalGenerations = totalGenerations,
                TotalCreditsUsed = totalCreditsUsed,
                TotalCreditsGranted = totalCreditsGranted
            });
        }

        public async Task<ServiceResult<AdminTransactionListDto>> GetUserTransactionsAsync(
            Guid userId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < MinPageSize) pageSize = DefaultPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var userExists = await _db.Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
                return ServiceResult<AdminTransactionListDto>.Failure("Kullanıcı bulunamadı.", 404);

            var query = _db.TokenTransactions.AsNoTracking()
                .Where(t => t.UserId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    t.Id,
                    t.CreatedAt,
                    t.Amount,
                    t.Type,
                    t.Description,
                    t.AdminUserId,
                    t.AdminNote
                })
                .ToListAsync(cancellationToken);

            // Admin email — batch (transaction'lardaki AdminUserId'lerin email'i).
            var adminIds = transactions
                .Where(t => t.AdminUserId.HasValue)
                .Select(t => t.AdminUserId!.Value)
                .Distinct()
                .ToList();

            var adminEmailDict = adminIds.Count > 0
                ? await _db.Users.AsNoTracking()
                    .Where(u => adminIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Email ?? string.Empty, cancellationToken)
                : new Dictionary<Guid, string>();

            var items = transactions.Select(t => new AdminTransactionDto
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Description = t.Description,
                AdminUserId = t.AdminUserId,
                AdminEmail = t.AdminUserId.HasValue && adminEmailDict.TryGetValue(t.AdminUserId.Value, out var email)
                    ? email
                    : null,
                AdminNote = t.AdminNote
            }).ToList();

            return ServiceResult<AdminTransactionListDto>.Success(new AdminTransactionListDto
            {
                Transactions = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<AdminDashboardStatsDto>> GetDashboardStatsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var weekStart = todayStart.AddDays(-7);

            var totalUsers = await _db.Users.AsNoTracking()
                .CountAsync(cancellationToken);

            var usersToday = await _db.Users.AsNoTracking()
                .CountAsync(u => u.CreatedAt >= todayStart, cancellationToken);

            var usersThisWeek = await _db.Users.AsNoTracking()
                .CountAsync(u => u.CreatedAt >= weekStart, cancellationToken);

            // Üretim — Generations tablosu (gerçek kaynak).
            var generationsToday = await _db.Generations.AsNoTracking()
                .CountAsync(g => g.CreatedAt >= todayStart, cancellationToken);

            var generationsThisWeek = await _db.Generations.AsNoTracking()
                .CountAsync(g => g.CreatedAt >= weekStart, cancellationToken);

            // Kredi tüketimi — negatif transaction toplamı (mutlak).
            var creditsUsedToday = await _db.TokenTransactions.AsNoTracking()
                .Where(t => t.Amount < 0 && t.CreatedAt >= todayStart)
                .SumAsync(t => -t.Amount, cancellationToken);

            var creditsUsedThisWeek = await _db.TokenTransactions.AsNoTracking()
                .Where(t => t.Amount < 0 && t.CreatedAt >= weekStart)
                .SumAsync(t => -t.Amount, cancellationToken);

            var creditsGrantedByAdminsThisWeek = await _db.TokenTransactions.AsNoTracking()
                .Where(t => t.Amount > 0 && t.AdminUserId != null && t.CreatedAt >= weekStart)
                .SumAsync(t => t.Amount, cancellationToken);

            return ServiceResult<AdminDashboardStatsDto>.Success(new AdminDashboardStatsDto
            {
                TotalUsers = totalUsers,
                UsersToday = usersToday,
                UsersThisWeek = usersThisWeek,
                GenerationsToday = generationsToday,
                GenerationsThisWeek = generationsThisWeek,
                CreditsUsedToday = creditsUsedToday,
                CreditsUsedThisWeek = creditsUsedThisWeek,
                CreditsGrantedByAdminsThisWeek = creditsGrantedByAdminsThisWeek,
                GeneratedAt = now
            });
        }

        /// <summary>
        /// AdminOptions.AllowedEmails'ten normalize edilmiş (trim + lower) email HashSet'i kurar.
        /// Batch admin kontrolü için (Identity Role sistemi YOK).
        /// </summary>
        private HashSet<string> BuildAdminEmailSet()
        {
            return _adminOptions.AllowedEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .ToHashSet();
        }

        private static bool IsAdminEmail(string? email, HashSet<string> adminSet)
        {
            return !string.IsNullOrWhiteSpace(email)
                && adminSet.Contains(email.Trim().ToLowerInvariant());
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
