namespace SelfAI.Entities
{
    public class Package
    {
        public int Id { get; set; }
        public string Name { get; set; }              // "Free", "Basic", "Plus", "Premium"
        public int MonthlyCredits { get; set; }
        public decimal? PriceTry { get; set; }         // null = ücretsiz
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
