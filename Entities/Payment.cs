namespace SelfAI.Entities
{
    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Refunded = 4
    }

    public class Payment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }

        // Iyzico tarafı
        public string IyzicoConversationId { get; set; }   // Initiate'te yaratılır, unique
        public string IyzicoPaymentId { get; set; }        // Tamamlanınca dolar
        public string IyzicoToken { get; set; }            // Checkout token (callback için)

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser User { get; set; }
        public Subscription Subscription { get; set; }
    }
}
