using SelfAI.DTOs.RenderNetResourceDtos;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetResourcesService
    {
        // RenderNet karakterlerini(stillerini) API'den çeker
        Task<FluxImageStyleRootDto> GetFluxStylesAsync();
    }
}
