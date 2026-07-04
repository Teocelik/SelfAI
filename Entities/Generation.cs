namespace SelfAI.Entities
{
    public enum GenerationStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Refunded = 4
    }

    public class Generation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        // Provider generation ID (fal.ai queue request ID). İsim geçmiş provider'dan kalma,
        // F.7'de rename edilebilir (DB kolonu canlı veri uyumu için korunuyor).
        public string RenderNetGenerationId { get; set; }
        public int Cost { get; set; }
        public GenerationStatus Status { get; set; } = GenerationStatus.Pending;
        public string PromptSnapshot { get; set; }   // opsiyonel: prompt'un snapshot'ı
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public AppUser User { get; set; }
        public ICollection<GenerationMedia> MediaItems { get; set; } = new List<GenerationMedia>();
    }
}
