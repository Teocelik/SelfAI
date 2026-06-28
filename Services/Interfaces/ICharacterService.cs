using SelfAI.DTOs.Characters;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    // Karakter listeleme + arşivleme business servisi (F.6.1 + F.M.4).
    // Oluşturma artık ICharacterTrainingOrchestrator'da (fal.ai LoRA training).
    public interface ICharacterService
    {
        Task<ServiceResult<CharacterListResponse>> GetUserCharactersAsync(
            Guid userId, int page = 1, int pageSize = 20);

        Task<ServiceResult<bool>> ArchiveCharacterAsync(
            Guid userId, Guid characterId);
    }
}
