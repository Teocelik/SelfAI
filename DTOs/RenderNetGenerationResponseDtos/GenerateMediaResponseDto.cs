using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetGenerationResponseDtos
{
    /// <summary>
    /// POST /generations yanıtı
    /// Görsel oluşturma isteği kabul edildiğinde dönen DTO
    /// 
    /// ⚠️ Bu yanıtta URL YOK! 
    /// Çünkü görsel henüz oluşturulmadı, sadece "kabul edildi" (initiated).
    /// URL'i almak için Get Generation ile POLLING yapılmalı.
    /// </summary>
    public class GenerateMediaResponseDto
    {
        [JsonPropertyName("data")]
        public GenerateMediaDataDto Data { get; set; }

        [JsonPropertyName("err")]
        public object Err { get; set; }
    }

    public class GenerateMediaDataDto
    {
        [JsonPropertyName("credits_remaining")]
        public int CreditsRemaining { get; set; }

        [JsonPropertyName("generation_id")]
        public string GenerationId { get; set; }

        [JsonPropertyName("media")]
        public List<GenerateMediaItemDto> Media { get; set; }

        /// <summary>
        /// "initiated" = İşlem başlatıldı
        /// </summary>
        [JsonPropertyName("result")]
        public string Result { get; set; }
    }

    public class GenerateMediaItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("dim")]
        public MediaDimensionDto Dim { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        /// <summary>
        /// "initiated" → henüz işleme alınmadı
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("style")]
        public string Style { get; set; }

        [JsonPropertyName("style_detail")]
        public MediaStyleDetailDto StyleDetail { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class MediaDimensionDto
    {
        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }

    public class MediaStyleDetailDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("base_model")]
        public string BaseModel { get; set; }
    }
}
