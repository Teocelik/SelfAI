namespace SelfAI.Entities
{
    public class GenerationMedia
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GenerationId { get; set; }
        public string Url { get; set; }                   // Affogato'dan dönen URL
        public string MediaType { get; set; } = "image";  // "image" veya "video"
        public int Order { get; set; }                    // Multi-model'de sıralama (0, 1, 2...)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Generation Generation { get; set; }
    }
}
