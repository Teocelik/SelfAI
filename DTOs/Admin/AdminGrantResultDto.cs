namespace SelfAI.DTOs.Admin
{
    /// <summary>
    /// Admin kredi ekleme işleminin sonucunu taşıyan DTO.
    /// </summary>
    public class AdminGrantResultDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public int PreviousBalance { get; set; }
        public int NewBalance { get; set; }
        public int Amount { get; set; }
        public string? Note { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
