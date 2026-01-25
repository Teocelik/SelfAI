using System.Text.Json.Serialization;

namespace SelfAI.DTOs.RenderNetResourceDtos
{
    public class FluxImageStyleRootDto
    {
        public List<FluxImageSytleDetailDto> StyleDetail { get; set; }
    }
}
