using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.RenderNet;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IRenderNetAssetService
    {
        // RenderNet API'sine varlık yükleme işlemi için gerekli metot
        Task<string> UploadAssetAsync(object payload);

        // RenderNet API'sine varlık alma işlemi için gerekli metot
        Task<string> GetAssetAsync(UploadAssetResponse asset);

        // Upload url almak için gerekli metot imzası
        Task<UploadAssetResponse> GetUploadUrlAsync();
    }
}
