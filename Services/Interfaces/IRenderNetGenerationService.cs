using SelfAI.DTOs.RenderNetGenerationRequestDtos;
using SelfAI.DTOs.RenderNetGenerationResponseDtos; 
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
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