namespace SelfAI.Entities
{
    public enum TokenTransactionType
    {
        BonusGrant = 1,        // Kayıt sonrası 5 credit gibi
        MonthlyReset = 2,      // Subscription yenilenince (D.3'te)
        GenerationDebit = 3,   // Generate harcaması (D.2'de kullanılacak)
        GenerationRefund = 4,  // Generation (fal.ai) hata olursa iade (D.2'de)
        ManualAdjust = 5,      // Admin tarafından elle düzeltme
        SubscriptionGrant = 6  // Abonelik ödemesi sonrası kredi yüklemesi (D.3.2'de)
    }

    public class TokenTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public int Amount { get; set; }                // + ekleme, - harcama
        public TokenTransactionType Type { get; set; }
        public string Description { get; set; }        // opsiyonel açıklama
        public Guid? RelatedGenerationId { get; set; } // D.2'de Generation entity gelince ilişki kurulur
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser User { get; set; }
    }
}
