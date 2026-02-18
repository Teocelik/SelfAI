using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetResourceDtos
{
    public class FluxImageStyleRootDto
    {
        [JsonPropertyName("data")]
        public List<FluxImageSytleDetailDto> Data { get; set; }

        [JsonPropertyName("err")]
        public object Err { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationDto Pagination { get; set; }
    }

    public class PaginationDto
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
