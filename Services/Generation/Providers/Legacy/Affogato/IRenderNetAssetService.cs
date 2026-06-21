using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.Legacy.RenderNet.Upload;
using SelfAI.DTOs.Legacy.RenderNet.Generation;
using SelfAI.Models;

namespace SelfAI.Services.Generation.Providers.Legacy.Affogato
{
    public interface IRenderNetAssetService
    {
        // Upload url almak için gerekli metot imzası
        //Task<UploadAssetResponseDto> GetUploadUrlAsync();

        // Varlık yükleme işlemi için gerekli metot imzası
        //Task<UploadAssetResponseDto> UploadAssetAsync(MediaGenerationRequestDto imageFile);

        Task<ServiceResult<UploadAssetResponseDto>> GetAssetIdAsync(UploadAssetRequestDto request);
    }
}
