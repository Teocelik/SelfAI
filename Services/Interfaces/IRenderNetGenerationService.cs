using SelfAI.DTOs.RenderNetGenerationRequestDtos;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetGenerationService
    {
        Task<string> GenerateMediaAsync(MediaGenerationRequestDto dto);
    }
}
