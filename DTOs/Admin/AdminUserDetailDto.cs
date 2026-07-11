namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// F.9a — Kullanıcı detay sayfası bilgi kartı. Soft-delete alanı AppUser'da olmadığı için
    /// IsDeleted her zaman false döner (F.9b'de soft-delete gelirse doldurulur).
    /// </summary>
    public class AdminUserDetailDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int CurrentBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDeleted { get; set; }
        public int TotalGenerations { get; set; }
        public int TotalCreditsUsed { get; set; }
        public int TotalCreditsGranted { get; set; }   // Admin tarafından eklenen toplam (AdminUserId != null)
    }
}
