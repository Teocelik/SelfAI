using SelfAI.DTOs.Characters;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    // Karakter yönetimi business servisi: DB + Affogato API orkestrasyonu (F.6.1).
    public interface ICharacterService
    {
        Task<ServiceResult<CharacterListResponse>> GetUserCharactersAsync(
            Guid userId, int page = 1, int pageSize = 20);

        Task<ServiceResult<CharacterDto>> CreateCharacterAsync(
            Guid userId, CharacterCreateRequest request);

        Task<ServiceResult<bool>> ArchiveCharacterAsync(
            Guid userId, Guid characterId);
    }
}
