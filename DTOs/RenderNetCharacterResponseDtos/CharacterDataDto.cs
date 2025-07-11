using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetCharacterResponseDtos
{
    public class CharacterDataDto
    {
        [JsonPropertyName("character_type")]
        public string CharacterType { get; set; }

        [JsonPropertyName("input_image")]
        public string InputImage { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
