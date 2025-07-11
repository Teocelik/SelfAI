using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.RenderNet;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetAssetService
    {
        // Upload url almak için gerekli metot imzası
        Task<UploadAssetResponseDto> GetUploadUrlAsync();

        // Varlık yükleme işlemi için gerekli metot imzası
        Task<UploadAssetResponseDto> UploadAssetAsync(IFormFile imageFile);
    }
}
