namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// F.9a — Paginated kullanıcı listesinde tek satır. Batch lookup ile doldurulur (N+1 yok).
    /// </summary>
    public class AdminUserListItemDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int CurrentBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int TotalGenerations { get; set; }   // Generations tablosundan
        public int TotalCreditsUsed { get; set; }   // Negatif transaction toplamı (mutlak)
    }
}
