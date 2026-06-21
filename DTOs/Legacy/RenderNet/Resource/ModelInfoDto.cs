using System.Text.Json.Serialization;

namespace SelfAI.DTOs.Legacy.RenderNet.Resource
{
    // Tek bir RenderNet modelini temsil eder (FluxImageSytleDetailDto ile bire bir aynı kalıp)
    public class ModelInfoDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("base_model")]
        public string BaseModel { get; set; }
    }

    // API yanıtının kök sarmalayıcısı: { data: [...], err, pagination }
    // PaginationDto, FluxImageStyleRootDto ile aynı namespace'te tanımlı; yeniden kullanılıyor.
    public class ModelRootDto
    {
        [JsonPropertyName("data")]
        public List<ModelInfoDto> Data { get; set; }

        [JsonPropertyName("err")]
        public object Err { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationDto Pagination { get; set; }
    }
}
