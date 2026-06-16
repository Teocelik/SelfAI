using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetCharacterResponseDtos
{
    // POST /pub/v1/characters yanıtının kök sarmalayıcısı.
    // List yanıtından farkı: "data" tek bir karakter objesidir (array değil).
    public class CharacterCreateResponseDto
    {
        [JsonPropertyName("data")]
        public CharacterDataDto Data { get; set; }

        [JsonPropertyName("err")]
        public object Err { get; set; }
    }
}
