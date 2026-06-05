using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.Entities;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    // Abonelik yaşam döngüsü: başlatma (Pending), aktive etme (ödeme başarılı), iptal (ödeme başarısız).
    // İş mantığı burada; Controller yalnızca HTTP/transport ile ilgilenir.
    public class SubscriptionService : ISubscriptionService
    {
        private const int SUBSCRIPTION_DURATION_DAYS = 30;

        private readonly AppDbContext _db;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(AppDbContext db, ILogger<SubscriptionService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ServiceResult<(Subscription Subscription, Payment Payment)>> InitiateSubscriptionAsync(
            Guid userId, int packageId)
        {
            var package = await _db.Packages.FirstOrDefaultAsync(p => p.Id == packageId);
            if (package == null || !package.IsActive)
            {
                _logger.LogWarning(
                    "Abonelik başlatılamadı — paket bulunamadı/pasif. | UserId: {Uid} | PackageId: {Pid}",
                    userId, packageId);
                return ServiceResult<(Subscription, Payment)>.Failure("Paket bulunamadı veya aktif değil.", 404);
            }

            // Free (ücretsiz) paket subscribe edilemez — kayıt bonusu olarak verilir.
            if (package.PriceTry == null || package.PriceTry.Value <= 0m)
            {
                return ServiceResult<(Subscription, Payment)>.Failure(
                    "Ücretsiz paket için abonelik başlatılamaz.", 400);
            }

            var now = DateTime.UtcNow;

            // MVP davranışı: mevcut aktif abonelik(ler) iptal edilir (proration yok).
            var activeSubscriptions = await _db.Subscriptions
                .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
                .ToListAsync();

            foreach (var active in activeSubscriptions)
            {
                active.Status = SubscriptionStatus.Cancelled;
                active.UpdatedAt = now;
                _logger.LogInformation(
                    "Mevcut aktif abonelik iptal edildi (yeni abonelik başlatılıyor). | UserId: {Uid} | SubId: {SubId}",
                    userId, active.Id);
            }

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PackageId = package.Id,
                Status = SubscriptionStatus.Pending,
                AutoRenew = false,
                CreatedAt = now,
                UpdatedAt = now
                // StartDate / EndDate aktivasyonda set edilecek (Pending'de default kalır).
            };

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = subscription.Id,
                IyzicoConversationId = Guid.NewGuid().ToString(),
                IyzicoPaymentId = string.Empty,  // NOT NULL kolon — aktivasyonda doldurulur
                IyzicoToken = string.Empty,      // NOT NULL kolon — checkout başlatıldıktan sonra doldurulur
                Amount = package.PriceTry.Value,
                Currency = "TRY",
                Status = PaymentStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Subscriptions.Add(subscription);
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Abonelik + ödeme kaydı oluşturuldu (Pending). | UserId: {Uid} | SubId: {SubId} | ConvId: {ConvId} | Paket: {Paket}",
                userId, subscription.Id, payment.IyzicoConversationId, package.Name);

            return ServiceResult<(Subscription, Payment)>.Success((subscription, payment));
        }

        public async Task<ServiceResult<Subscription>> ActivateSubscriptionAsync(
            string conversationId, string iyzicoPaymentId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return ServiceResult<Subscription>.Failure("Ödeme referansı (conversationId) eksik.", 400);
            }

            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.IyzicoConversationId == conversationId);

            if (payment == null)
            {
                _logger.LogError(
                    "Aktivasyon başarısız — Payment bulunamadı. | ConvId: {ConvId}", conversationId);
                return ServiceResult<Subscription>.Failure("Ödeme kaydı bulunamadı.", 404);
            }

            // Idempotensi: callback iki kez gelirse aynı işlem tekrar uygulanmasın.
            if (payment.Status == PaymentStatus.Completed)
            {
                _logger.LogWarning(
                    "Aktivasyon atlandı — Payment zaten Completed. | ConvId: {ConvId}", conversationId);
                var alreadyActive = await _db.Subscriptions
                    .Include(s => s.Package)
                    .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId);
                return alreadyActive != null
                    ? ServiceResult<Subscription>.Success(alreadyActive, "Abonelik zaten aktif.")
                    : ServiceResult<Subscription>.Failure("Abonelik kaydı bulunamadı.", 404);
            }

            var subscription = await _db.Subscriptions
                .Include(s => s.Package)
                .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId);

            if (subscription == null || subscription.Package == null)
            {
                _logger.LogError(
                    "Aktivasyon başarısız — Subscription/Package bulunamadı. | ConvId: {ConvId} | SubId: {SubId}",
                    conversationId, payment.SubscriptionId);
                return ServiceResult<Subscription>.Failure("Abonelik kaydı bulunamadı.", 404);
            }

            var package = subscription.Package;
            var now = DateTime.UtcNow;
            var nextReset = now.AddDays(SUBSCRIPTION_DURATION_DAYS);

            // 1. Payment'ı tamamla
            payment.Status = PaymentStatus.Completed;
            payment.IyzicoPaymentId = iyzicoPaymentId ?? string.Empty;
            payment.PaidAt = now;
            payment.UpdatedAt = now;

            // 2. Subscription'ı aktive et (30 gün sabit süre)
            subscription.Status = SubscriptionStatus.Active;
            subscription.StartDate = now;
            subscription.EndDate = nextReset;
            subscription.UpdatedAt = now;

            // 3. Cüzdana kredi yükle (top-up)
            var wallet = await _db.TokenWallets.FirstOrDefaultAsync(w => w.UserId == subscription.UserId);
            if (wallet == null)
            {
                _logger.LogError(
                    "Aktivasyon başarısız — kullanıcı cüzdanı bulunamadı. | UserId: {Uid}", subscription.UserId);
                return ServiceResult<Subscription>.Failure("Kullanıcı cüzdanı bulunamadı.", 404);
            }

            wallet.CurrentBalance += package.MonthlyCredits;
            wallet.MonthlyAllowance = package.MonthlyCredits;
            wallet.LastResetDate = now;
            wallet.NextResetDate = nextReset;
            wallet.UpdatedAt = now;

            // 4. TokenTransaction audit kaydı
            _db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                UserId = subscription.UserId,
                Amount = package.MonthlyCredits,
                Type = TokenTransactionType.SubscriptionGrant,
                Description = $"{package.Name} subscription aktif",
                CreatedAt = now
            });

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Abonelik aktive edildi + cüzdan yüklendi. | UserId: {Uid} | SubId: {SubId} | Paket: {Paket} | Kredi: +{Kredi} | YeniBakiye: {Bakiye}",
                subscription.UserId, subscription.Id, package.Name, package.MonthlyCredits, wallet.CurrentBalance);

            return ServiceResult<Subscription>.Success(subscription, "Abonelik aktive edildi.");
        }

        public async Task<ServiceResult<int>> CancelPendingSubscriptionAsync(string conversationId, string reason)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                _logger.LogWarning("İptal atlandı — conversationId eksik. | Reason: {Reason}", reason);
                return ServiceResult<int>.Failure("Ödeme referansı (conversationId) eksik.", 400);
            }

            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.IyzicoConversationId == conversationId);

            if (payment == null)
            {
                _logger.LogWarning(
                    "İptal atlandı — Payment bulunamadı. | ConvId: {ConvId} | Reason: {Reason}",
                    conversationId, reason);
                return ServiceResult<int>.Failure("Ödeme kaydı bulunamadı.", 404);
            }

            var now = DateTime.UtcNow;
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = now;

            var subscription = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId);
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.UpdatedAt = now;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Bekleyen abonelik iptal edildi (ödeme başarısız). | ConvId: {ConvId} | SubId: {SubId} | Reason: {Reason}",
                conversationId, payment.SubscriptionId, reason);

            // Hiçbir cüzdan/kredi hareketi YAPILMADI.
            return ServiceResult<int>.Success(0, "Bekleyen abonelik iptal edildi.");
        }
    }
}
