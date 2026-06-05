namespace SelfAI.Entities
{
    public class TokenWallet
    {
        public Guid UserId { get; set; }              // PK + FK to AppUser
        public int CurrentBalance { get; set; }       // şu anki kredi
        public int MonthlyAllowance { get; set; }     // aktif paketten gelir, Free için 5 (one-time)
        public DateTime? LastResetDate { get; set; }
        public DateTime? NextResetDate { get; set; }  // Free için null (yenilenmez)
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public AppUser User { get; set; }
    }
}
