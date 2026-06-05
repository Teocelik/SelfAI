using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.Entities;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class CreditService : ICreditService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CreditService> _logger;

        public CreditService(AppDbContext db, ILogger<CreditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> GetBalanceAsync(Guid userId)
        {
            var wallet = await _db.TokenWallets
                .Where(w => w.UserId == userId)
                .Select(w => new { w.CurrentBalance })
                .FirstOrDefaultAsync();

            return wallet?.CurrentBalance ?? 0;
        }

        public async Task<ServiceResult<int>> TryDeductAsync(Guid userId, int amount, string description)
        {
            if (amount <= 0)
            {
                return ServiceResult<int>.Failure("Geçersiz miktar.", 400);
            }

            // ATOMİK düşüm: WHERE CurrentBalance >= amount
            // Race condition güvenli (tek SQL UPDATE, satır kilidi DB tarafında)
            var rowsAffected = await _db.TokenWallets
                .Where(w => w.UserId == userId && w.CurrentBalance >= amount)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(w => w.CurrentBalance, w => w.CurrentBalance - amount)
                    .SetProperty(w => w.UpdatedAt, w => DateTime.UtcNow));

            if (rowsAffected == 0)
            {
                _logger.LogWarning(
                    "Yetersiz bakiye veya kullanıcı bulunamadı. | UserId: {Uid} | Amount: {Amt}",
                    userId, amount);
                return ServiceResult<int>.Failure("Yetersiz kredi bakiyesi.", 402);
            }

            // Transaction kayıt
            var transaction = new TokenTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = -amount,
                Type = TokenTransactionType.GenerationDebit,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            _db.TokenTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            var newBalance = await GetBalanceAsync(userId);

            _logger.LogInformation(
                "Kredi düşüldü. | UserId: {Uid} | Amount: {Amt} | NewBalance: {Bal}",
                userId, amount, newBalance);

            return ServiceResult<int>.Success(newBalance, "Kredi düşüldü.");
        }

        public async Task<ServiceResult<int>> RefundAsync(Guid userId, int amount, string reason, Guid? relatedGenerationId = null)
        {
            if (amount <= 0)
            {
                return ServiceResult<int>.Failure("Geçersiz miktar.", 400);
            }

            var rowsAffected = await _db.TokenWallets
                .Where(w => w.UserId == userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(w => w.CurrentBalance, w => w.CurrentBalance + amount)
                    .SetProperty(w => w.UpdatedAt, w => DateTime.UtcNow));

            if (rowsAffected == 0)
            {
                _logger.LogError(
                    "Refund başarısız — cüzdan bulunamadı. | UserId: {Uid} | Amount: {Amt}",
                    userId, amount);
                return ServiceResult<int>.Failure("Cüzdan bulunamadı.", 404);
            }

            var transaction = new TokenTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = amount,
                Type = TokenTransactionType.GenerationRefund,
                Description = reason,
                RelatedGenerationId = relatedGenerationId,
                CreatedAt = DateTime.UtcNow
            };
            _db.TokenTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            var newBalance = await GetBalanceAsync(userId);

            _logger.LogInformation(
                "Kredi iadesi yapıldı. | UserId: {Uid} | Amount: {Amt} | NewBalance: {Bal} | Reason: {Reason}",
                userId, amount, newBalance, reason);

            return ServiceResult<int>.Success(newBalance, "Kredi iade edildi.");
        }
    }
}
