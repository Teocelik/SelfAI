using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.Entities;

namespace SelfAI.BackgroundServices
{
    public class SubscriptionLifecycleService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionLifecycleService> _logger;

        // Çalışma aralığı — production'da 1 saat, dev'de değiştirilebilir
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        // Stale Pending temizliği için eşik
        private readonly TimeSpan _stalePendingThreshold = TimeSpan.FromHours(1);

        public SubscriptionLifecycleService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionLifecycleService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Subscription Lifecycle Service başlatıldı. | Interval: {Interval}",
                _interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredSubscriptionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Expired subscription işlemi başarısız.");
                }

                try
                {
                    await ProcessStalePendingSubscriptionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stale pending subscription temizliği başarısız.");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Subscription Lifecycle Service durduruldu.");
        }

        /// <summary>
        /// EndDate'i geçmiş Active subscription'ları Expired'a alır.
        /// İlgili kullanıcıların wallet.NextResetDate'ini temizler.
        /// Wallet credit'lerine DOKUNMAZ (kullanıcı mevcut credit'lerini kullanmaya devam edebilir).
        /// </summary>
        private async Task ProcessExpiredSubscriptionsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;

            var expiredSubs = await db.Subscriptions
                .Include(s => s.User)
                    .ThenInclude(u => u.Wallet)
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < now)
                .ToListAsync(ct);

            if (expiredSubs.Count == 0)
            {
                return;
            }

            foreach (var sub in expiredSubs)
            {
                sub.Status = SubscriptionStatus.Expired;
                sub.UpdatedAt = now;

                if (sub.User?.Wallet != null)
                {
                    sub.User.Wallet.NextResetDate = null;
                    sub.User.Wallet.UpdatedAt = now;
                }

                _logger.LogInformation(
                    "Subscription Expired'a alındı. | SubId: {SubId} | UserId: {Uid} | EndDate: {EndDate}",
                    sub.Id, sub.UserId, sub.EndDate);
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Expired subscription işlemi tamamlandı. | Count: {Count}",
                expiredSubs.Count);
        }

        /// <summary>
        /// 1 saatten eski Pending subscription'ları temizler — kullanıcı checkout'u
        /// yarıda bırakmış olabilir. Subscription Cancelled, ilgili Pending Payment'lar Failed.
        /// </summary>
        private async Task ProcessStalePendingSubscriptionsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            var threshold = now.Subtract(_stalePendingThreshold);

            var stalePending = await db.Subscriptions
                .Include(s => s.Payments)
                .Where(s => s.Status == SubscriptionStatus.Pending && s.CreatedAt < threshold)
                .ToListAsync(ct);

            if (stalePending.Count == 0)
            {
                return;
            }

            foreach (var sub in stalePending)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.UpdatedAt = now;

                if (sub.Payments != null)
                {
                    foreach (var payment in sub.Payments.Where(p => p.Status == PaymentStatus.Pending))
                    {
                        payment.Status = PaymentStatus.Failed;
                        payment.UpdatedAt = now;
                    }
                }

                _logger.LogInformation(
                    "Stale Pending subscription Cancelled'a alındı. | SubId: {SubId} | UserId: {Uid} | CreatedAt: {CreatedAt}",
                    sub.Id, sub.UserId, sub.CreatedAt);
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Stale Pending temizliği tamamlandı. | Count: {Count}",
                stalePending.Count);
        }
    }
}
