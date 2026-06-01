using SelfAI.DTOs.RenderNetResourceDtos;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetResourcesService
    {
        // RenderNet karakterlerini(stillerini) API'den çeker
        Task<FluxImageStyleRootDto> GetFluxStylesAsync();

        // RenderNet modellerini API'den çeker
        Task<ServiceResult<IReadOnlyList<ModelInfoDto>>> GetModelsAsync(string type = "flux", int pageSize = 50);
    }
}
