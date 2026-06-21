using System.Text.Json.Serialization;

namespace SelfAI.DTOs.Legacy.RenderNet.Character
{
    // POST /pub/v1/characters istek gövdesi (Affogato/RenderNet API).
    public class CharacterCreateRequestDto
    {
        [JsonPropertyName("asset_id")]
        public string AssetId { get; set; }

        [JsonPropertyName("character_type")]
        public string CharacterType { get; set; }  // "realistic" | "stylized"

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }
    }
}
