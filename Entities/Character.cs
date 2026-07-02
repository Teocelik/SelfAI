using SelfAI.Entities.Enums;

namespace SelfAI.Entities
{
    public class Character
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // AppUser FK — karakterin sahibi
        public Guid UserId { get; set; }

        // ⚠️ DEPRECATED (F.M.8'de silinir) — Affogato'nun döndürdüğü character id.
        // fal.ai karakterlerinde null olur (artık required değil).
        public string? AffogatoCharacterId { get; set; }

        // ⚠️ DEPRECATED (F.M.8'de silinir) — Affogato'ya gönderilen unique name.
        public string? AffogatoCharacterName { get; set; }

        // Kullanıcı dostu görünür isim ("Mira")
        public string Name { get; set; }

        // Karakter açıklaması (generation prompt'una eklenir)
        public string Prompt { get; set; }

        public CharacterType CharacterType { get; set; }

        // Modal thumbnail için. fal.ai karakterlerinde ilk yüz görselinin URL'i set edilir.
        public string? ThumbnailUrl { get; set; }

        public CharacterStatus Status { get; set; } = CharacterStatus.Active;

        // ═══ F.M.4 — fal.ai LoRA training alanları ═══

        // fal.ai training output — eğitilmiş LoRA ağırlık dosyasının URL'i (.safetensors).
        public string? LoraModelUrl { get; set; }

        // Eğitimin yaşam döngüsü durumu (DB'de int).
        public LoraTrainingStatus LoraTrainingStatus { get; set; } = LoraTrainingStatus.Pending;

        // fal.ai queue request_id — polling/status sorgusu için.
        public string? LoraTrainingJobId { get; set; }

        // Generation prompt'una eklenecek tetikleyici kelime (örn. "SLF_X3K9MZ").
        public string? TriggerWord { get; set; }

        public DateTime? TrainingStartedAt { get; set; }
        public DateTime? TrainingCompletedAt { get; set; }

        // Training başarısızsa kullanıcıya gösterilecek güvenli sebep.
        public string? TrainingFailureReason { get; set; }

        // F.M.7 — training input yüz görsellerinin Asset ID'leri (DB'de JSON nvarchar(max)).
        // Eski FaceReferenceUrls (inline URL) kaldırıldı; artık Asset entity üzerinden resolve edilir.
        public List<Guid> FaceReferenceAssetIds { get; set; } = new();

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
        Archived = 2,
        // F.M.4 — eski Affogato karakterleri bu durumla gizlenir (modal'da gösterilmez).
        Migrated = 3
    }
}
