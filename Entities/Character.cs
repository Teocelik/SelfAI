namespace SelfAI.Entities
{
    public class Character
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // AppUser FK — karakterin sahibi
        public Guid UserId { get; set; }

        // Affogato'nun döndürdüğü character id
        public string AffogatoCharacterId { get; set; }

        // Affogato'ya gönderdiğimiz unique name (suffix'li, örn "Mira-a3f7b2c1")
        public string AffogatoCharacterName { get; set; }

        // Kullanıcı dostu görünür isim ("Mira")
        public string Name { get; set; }

        // Karakter açıklaması (Affogato prompt field'ı)
        public string Prompt { get; set; }

        public CharacterType CharacterType { get; set; }

        // Affogato create response'unda dönen input_image URL'i (modal thumbnail için).
        // Opsiyonel — API yanıtında gelmeyebilir, bu yüzden nullable.
        public string? ThumbnailUrl { get; set; }

        public CharacterStatus Status { get; set; } = CharacterStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }

        // Navigation
        public AppUser User { get; set; }
    }

    public enum CharacterType
    {
        Realistic = 1,
        Stylized = 2
    }

    public enum CharacterStatus
    {
        Active = 1,
        Archived = 2
    }
}
