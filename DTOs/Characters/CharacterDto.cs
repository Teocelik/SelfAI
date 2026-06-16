namespace SelfAI.DTOs.Characters
{
    // Frontend'e dönen karakter temsili (modal listesi + create sonucu).
    public class CharacterDto
    {
        // F.6.5 — artık Guid değil string: kullanıcı karakterleri için Guid.ToString(),
        // sistem karakterleri için Affogato'nun "chr_xxx" ID'si. Generation endpoint
        // CharacterId'yi zaten string aldığı için her iki format da uyumlu.
        public string Id { get; set; }
        public string Name { get; set; }
        public string Prompt { get; set; }
        public string ThumbnailUrl { get; set; }
        public string CharacterType { get; set; }  // "realistic" | "stylized"

        // F.6.5 — true ise Affogato'nun default sistem karakteri (DB'de yok, arşivlenemez).
        public bool IsSystemCharacter { get; set; }

        // Sistem karakterleri için null (oluşturulma tarihi bilinmiyor).
        public DateTime? CreatedAt { get; set; }
    }

    // POST /Characters/Create istek gövdesi.
    public class CharacterCreateRequest
    {
        public string Name { get; set; }
        public string Prompt { get; set; }
        public string CharacterType { get; set; }  // "realistic" | "stylized"
        public string AssetId { get; set; }         // Önceden /RenderNet/GetAssetId ile upload edilmiş face image
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
