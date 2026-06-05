using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.DTOs.AuthDtos;
using SelfAI.Entities;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UserService> _logger;
        private const int FREE_TIER_BONUS = 5;

        public UserService(AppDbContext db, ILogger<UserService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ServiceResult<AppUser>> GetOrCreateUserAsync(FirebaseUserInfoDto firebaseUser)
        {
            if (firebaseUser == null || string.IsNullOrWhiteSpace(firebaseUser.Uid))
            {
                return ServiceResult<AppUser>.Failure("Firebase kullanıcı bilgisi geçersiz.", 400);
            }

            var existingUser = await _db.Users
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUser.Uid);

            if (existingUser != null)
            {
                // Mevcut kullanıcı — login bilgilerini güncelle
                existingUser.LastLoginAt = DateTime.UtcNow;
                existingUser.UpdatedAt = DateTime.UtcNow;

                // Email/Picture/Name değişmiş olabilir, güncelle
                if (!string.IsNullOrEmpty(firebaseUser.Email)) existingUser.Email = firebaseUser.Email;
                if (!string.IsNullOrEmpty(firebaseUser.Name)) existingUser.DisplayName = firebaseUser.Name;
                if (!string.IsNullOrEmpty(firebaseUser.Picture)) existingUser.PictureUrl = firebaseUser.Picture;
                existingUser.EmailVerified = firebaseUser.EmailVerified;

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Mevcut kullanıcı login. | UserId: {Uid} | FirebaseUid: {Fid}",
                    existingUser.Id, existingUser.FirebaseUid);

                return ServiceResult<AppUser>.Success(existingUser, "Kullanıcı bulundu");
            }

            // Yeni kullanıcı — oluştur + cüzdan + bonus credit
            var newUser = new AppUser
            {
                Id = Guid.NewGuid(),
                FirebaseUid = firebaseUser.Uid,
                Email = firebaseUser.Email ?? string.Empty,
                DisplayName = firebaseUser.Name ?? string.Empty,
                PictureUrl = firebaseUser.Picture ?? string.Empty,
                EmailVerified = firebaseUser.EmailVerified,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            var wallet = new TokenWallet
            {
                UserId = newUser.Id,
                CurrentBalance = FREE_TIER_BONUS,
                MonthlyAllowance = FREE_TIER_BONUS,
                LastResetDate = DateTime.UtcNow,
                NextResetDate = null,   // Free tier yenilenmez
                UpdatedAt = DateTime.UtcNow
            };

            var bonusTransaction = new TokenTransaction
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                Amount = FREE_TIER_BONUS,
                Type = TokenTransactionType.BonusGrant,
                Description = "Kayıt bonusu — Free tier",
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(newUser);
            _db.TokenWallets.Add(wallet);
            _db.TokenTransactions.Add(bonusTransaction);

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Yeni kullanıcı oluşturuldu + 5 free credit verildi. | UserId: {Uid} | Email: {Email}",
                newUser.Id, newUser.Email);

            return ServiceResult<AppUser>.Success(newUser, "Yeni kullanıcı oluşturuldu + 5 free credit");
        }
    }
}
