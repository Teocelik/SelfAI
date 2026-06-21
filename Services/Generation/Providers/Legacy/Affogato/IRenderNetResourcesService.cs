using SelfAI.DTOs.Legacy.RenderNet.Resource;
using SelfAI.Models;

namespace SelfAI.Services.Generation.Providers.Legacy.Affogato
{
    public interface IRenderNetResourcesService
    {
        // RenderNet stillerini API'den çeker
        Task<ServiceResult<IReadOnlyList<FluxImageSytleDetailDto>>> GetStylesAsync(string type = "flux", int pageSize = 50);

        // RenderNet modellerini API'den çeker
        Task<ServiceResult<IReadOnlyList<ModelInfoDto>>> GetModelsAsync(string type = "flux", int pageSize = 50);
    }
}
