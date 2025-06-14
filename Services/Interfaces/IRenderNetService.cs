using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetService
    {
        Task<string> UploadAssetAsync(object payload);
    }
}
