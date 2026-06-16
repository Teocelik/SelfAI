using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetCharacterResponseDtos
{
    public class CharacterDataDto
    {
        [JsonPropertyName("character_type")]
        public string CharacterType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("system_character")]
        public bool SystemCharacter { get; set; }

        // Referans görsel URL'i. Hem create hem GET list yanıtında döner (F.6.5'te
        // sistem karakter thumbnail'i olarak kullanılır); gelmezse null/boş kalabilir.
        [JsonPropertyName("input_image")]
        public string InputImage { get; set; }
    }
}
