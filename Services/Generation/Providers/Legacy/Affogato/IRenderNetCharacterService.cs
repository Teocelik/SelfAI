using SelfAI.DTOs.Legacy.RenderNet.Character;
using SelfAI.Models;

namespace SelfAI.Services.Generation.Providers.Legacy.Affogato
{
    public interface IRenderNetCharacterService
    {
        // Karakterleri API'den listeler (GET /pub/v1/characters).
        Task<ServiceResult<IReadOnlyList<CharacterDataDto>>> GetCharactersAsync(int page = 1, int pageSize = 50);

        // Yeni karakter oluşturur (POST /pub/v1/characters). Dönen data id + input_image içerir.
        Task<ServiceResult<CharacterDataDto>> CreateCharacterAsync(
            string assetId, string name, string prompt, string characterType);

        // Karakteri arşivler (DELETE /pub/v1/characters/{id}). F.6.4'te detaylandırılacak.
        Task<ServiceResult<bool>> ArchiveCharacterAsync(string characterId);
    }
}
