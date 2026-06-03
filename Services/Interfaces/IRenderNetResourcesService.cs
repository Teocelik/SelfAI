using SelfAI.DTOs.RenderNetResourceDtos;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetResourcesService
    {
        // RenderNet stillerini API'den çeker
        Task<ServiceResult<IReadOnlyList<FluxImageSytleDetailDto>>> GetStylesAsync(string type = "flux", int pageSize = 50);

        // RenderNet modellerini API'den çeker
        Task<ServiceResult<IReadOnlyList<ModelInfoDto>>> GetModelsAsync(string type = "flux", int pageSize = 50);
    }
}
