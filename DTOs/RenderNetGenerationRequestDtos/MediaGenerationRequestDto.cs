using SelfAI.DTOs.RenderNetCharacterResponseDtos;
using SelfAI.DTOs.RenderNetResourceDtos;
using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetGenerationRequestDtos
{
    public class MediaGenerationRequestDto
    {
        // 1. KULLANICININ YÜKLEDİĞİ DOSYA
        public IFormFile AssetFile { get; set; }

        // 2. GÖRSEL OLUŞTURMA PARAMETRELERİ

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
        public int Steps { get; set; } 
        public int Seed { get; set; } 

        public string Quality { get; set; } 
        public string Sampler { get; set; }

        // Prompt alanları
        public string PositivePrompt { get; set; }
        public string NegativePrompt { get; set; }

        [JsonPropertyName("character")]
        public CharacterDataDto CharacterData { get; set; }
    }
}
