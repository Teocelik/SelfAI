namespace SelfAI.DTOs.Characters
{
    // Frontend'e dönen karakter temsili (modal listesi + create sonucu).
    public class CharacterDto
    {
        // F.6.5 — artık Guid değil string: kullanıcı karakterleri için Guid.ToString().
        // (Geçmiş provider'da sistem karakterleri "chr_xxx" formatındaydı.) Generation
        // endpoint CharacterId'yi zaten string aldığı için her iki format da uyumlu.
        public string Id { get; set; }
        public string Name { get; set; }
        public string Prompt { get; set; }
        public string ThumbnailUrl { get; set; }
        public string CharacterType { get; set; }  // "realistic" | "stylized"

        // F.6.5 — true ise geçmiş provider'ın default sistem karakteri. F.M.4 sonrası daima false
        // (sistem karakteri kavramı kaldırıldı), frontend uyumluluğu için tutulur.
        public bool IsSystemCharacter { get; set; }

        // F.M.4 — LoRA training durumu: "Pending"|"Uploading"|"Training"|"Ready"|"Failed".
        public string TrainingStatus { get; set; } = "Ready";

        // F.M.4 — training başarısızsa kullanıcıya gösterilecek güvenli sebep.
        public string? FailureReason { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

    // POST /Characters/Archive istek gövdesi.
    public class CharacterArchiveRequest
    {
        public Guid CharacterId { get; set; }
    }

    // GET /Characters/List yanıtı.
    public class CharacterListResponse
    {
        public IReadOnlyList<CharacterDto> Items { get; set; }
        public int TotalCount { get; set; }
    }
}
