namespace SelfAI.Entities
{
    public enum SubscriptionStatus
    {
        Pending = 1,        // Ödeme bekleniyor
        Active = 2,         // Aktif
        Expired = 3,        // Süresi dolmuş
        Cancelled = 4       // İptal edilmiş
    }

    public class Subscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public int PackageId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;
        public bool AutoRenew { get; set; } = false;   // D.3.3'te kullanılır

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser User { get; set; }
        public Package Package { get; set; }
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
