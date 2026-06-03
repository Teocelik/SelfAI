using SelfAI.DTOs.RenderNetCharacterResponseDtos;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetCharacterService
    {
        // Karakterleri API'den listeler (GET /pub/v1/characters).
        Task<ServiceResult<IReadOnlyList<CharacterDataDto>>> GetCharactersAsync(int page = 1, int pageSize = 50);
    }
}
