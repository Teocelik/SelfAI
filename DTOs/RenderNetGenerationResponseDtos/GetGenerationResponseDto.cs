using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetGenerationResponseDtos
{
    /// <summary>
    /// GET /generations/{generation_id} yanıtı
    /// Polling ile durumu kontrol ederken kullanılır.
    /// 
    /// status: "initiated"  → Henüz başlamadı
    /// status: "processing" → İşleniyor (tahmini)
    /// status: "success"    → ✅ Tamamlandı, URL hazır!
    /// status: "failed"     → ❌ Başarısız
    /// </summary>
    public class GetGenerationResponseDto
    {
        [JsonPropertyName("data")]
        public GetGenerationDataDto Data { get; set; }

        [JsonPropertyName("err")]
        public object Err { get; set; }
    }

    public class GetGenerationDataDto
    {
        [JsonPropertyName("created_at")]
        public long CreatedAt { get; set; }

        [JsonPropertyName("generation_id")]
        public string GenerationId { get; set; }

        [JsonPropertyName("media")]
        public List<GetGenerationMediaItemDto> Media { get; set; }
    }

    public class GetGenerationMediaItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("dim")]
        public MediaDimensionDto Dim { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// "initiated" | "processing" | "success" | "failed"
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Görsel URL'i - SADECE status: "success" olduğunda dolu!
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}