namespace SelfAI.Entities
{
    public class AppUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Firebase UID — auth tarafından gelir, unique
        public string FirebaseUid { get; set; }

        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string PictureUrl { get; set; }
        public bool EmailVerified { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public TokenWallet Wallet { get; set; }
        public ICollection<TokenTransaction> Transactions { get; set; } = new List<TokenTransaction>();
    }
}
