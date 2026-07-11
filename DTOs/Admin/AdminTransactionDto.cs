namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// F.9a — Tek bir TokenTransaction satırı (kullanıcı detay geçmişi).
    /// NOT: TokenTransaction'da BalanceAfter alanı YOK → "sonraki bakiye" gösterilmez.
    /// </summary>
    public class AdminTransactionDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Admin audit bilgisi (varsa)
        public Guid? AdminUserId { get; set; }
        public string? AdminEmail { get; set; }   // Batch join ile doldurulur
        public string? AdminNote { get; set; }
    }
}
