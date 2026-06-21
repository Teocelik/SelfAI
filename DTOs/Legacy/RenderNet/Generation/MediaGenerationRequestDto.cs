using SelfAI.DTOs.Legacy.RenderNet.Resource;
using System.Text.Json.Serialization;

namespace SelfAI.DTOs.Legacy.RenderNet.Generation
{
    public class MediaGenerationRequestDto
    {
        // yüz kilidi için asset_id
        public string FaceLockAssetId { get; set; }
        // Pose referans görselinin asset_id'si (ControlNet/Pose Lock için).
        // Boş ise pose lock payload'a dahil edilmez.
        public string PoseLockAssetId { get; set; }
        //görüntü boyutu 
        public string AspectRatio { get; set; } 
        //oluşturulacak görsel sayısı
        public int BatchSize { get; set; }
        //cf_scale, prompt'un ne kadar sıkı takip edileceğini belirler(4 <= cf_scale <= 12 ideal değer aralığıdır.)
        public double CfgScale { get; set; } = 7.0;
        //AI Model parametresi
        public string Model { get; set; } 
        //görsel stilini tutar
        public string Style { get; set; }
        //Görsel stil detaylarını tutar
        [JsonPropertyName("style_detail")]
        public FluxImageSytleDetailDto StyleDetail { get; set; }
        //oluşturma adım sayısı
        public int Steps { get; set; } = 25;
        public int Seed { get; set; }

        public string Quality { get; set; } = "Plus";
        public string Sampler { get; set; } = "DPM++ 2M Karras";

        // Prompt alanları
        public string PositivePrompt { get; set; }
        // NSFW/uygunsuz içerik koruması için güvenli varsayılan. Kullanıcı form alanı eklenirse override eder.
        public string NegativePrompt { get; set; } = "nsfw, deformed, extra limbs, bad anatomy, deformed pupils, text, worst quality, jpeg artifacts, ugly, duplicate, morbid, mutilated";

        // Karakter seçimi (opsiyonel). Boşsa karakter olmadan üretim.
        public string CharacterId { get; set; }

        // API'nin character.mode değeri: "flexible" | "balanced" | "strong"
        public string CharacterMode { get; set; } = "balanced";
    }
}
