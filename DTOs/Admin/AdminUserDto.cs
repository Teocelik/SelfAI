namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// Admin panelinde kullanıcı arama sonucu için taşıyıcı DTO.
    /// </summary>
    public class AdminUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int CurrentBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }
    }
}
