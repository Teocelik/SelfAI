using SelfAI.Entities;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IGenerationLogService
    {
        Task<ServiceResult<Guid>> CreateAsync(Guid userId, string renderNetGenerationId, int cost, string promptSnapshot);
        Task<ServiceResult<int>> UpdateStatusAsync(string renderNetGenerationId, GenerationStatus status);
        Task<ServiceResult<int>> SaveMediaItemsAsync(string renderNetGenerationId, IEnumerable<string> urls, string mediaType = "image");
    }
}
