using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetResourceDtos
{
    public class FluxImageSytleDetailDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("base_model")]
        public string BaseModel { get; set; }
    }
}
