using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetCharacterResponseDtos
{
    // GET /pub/v1/characters yanıtının kök sarmalayıcısı.
    // FluxImageStyleRootDto ile aynı yapısal kalıp; pagination metadata şimdilik gerekmiyor.
    public class CharacterListResponseDto
    {
        [JsonPropertyName("data")]
        public IReadOnlyList<CharacterDataDto> Data { get; set; }
    }
}
