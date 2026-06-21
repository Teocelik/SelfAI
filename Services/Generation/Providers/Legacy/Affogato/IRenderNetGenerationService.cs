using SelfAI.DTOs.Legacy.RenderNet.Generation;
using SelfAI.DTOs.Legacy.RenderNet.GenerationResponse; 
using SelfAI.Models;

namespace SelfAI.Services.Generation.Providers.Legacy.Affogato
{
    public interface IRenderNetGenerationService
    {
        /// <summary>
        /// Görsel oluşturma isteği gönder
        /// Dönen generation_id ile polling yapılacak
        /// </summary>
        Task<ServiceResult<GenerateMediaResponseDto>> GenerateMediaAsync(MediaGenerationRequestDto dto);

        /// <summary>
        /// 🆕 Oluşturma durumunu kontrol et (Polling için)
        /// </summary>
        Task<ServiceResult<GetGenerationResponseDto>> GetGenerationAsync(string generationId);
    }
}