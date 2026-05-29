using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IPromptService
    {
        // Hazır rastgele prompt listesini döner (frontend "Surprise me" butonu için)
        ServiceResult<IReadOnlyList<string>> GetAll();
    }
}
