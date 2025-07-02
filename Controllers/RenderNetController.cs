using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SelfAI.DTOs.RenderNet;
using SelfAI.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SelfAI.Controllers
{
    public class RenderNetController : Controller
    {
        private readonly IRenderNetAssetService _renderNetAssetService;
        private readonly IRenderNetGenerationService _renderNetGenerationService;

        public RenderNetController(IRenderNetAssetService renderNetAssetService, IRenderNetGenerationService renderNetGenerationService)
        {
            _renderNetAssetService = renderNetAssetService;
            _renderNetGenerationService = renderNetGenerationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // UploadUrl'e görsel yüklemek için gerekli metot
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile formFile)
        {
            // uploadUrl alalım
            var uploadUrlResponse = await _renderNetAssetService.GetUploadUrlAsync();

            // varlığı UploadUrl'ye yükleyelim
            var uploadImageResponse = await _renderNetAssetService.UploadAssetAsync(formFile);

            // Yükleme başarılı ise, GenerateImage metodunu çağırabiliriz

            return RedirectToAction("GenerateImage", new { assetId = uploadImageResponse.Data.Asset.Id});
        }

        //Kullanıcının görsel oluşturması için..
        // kullanıcı promptu veya hazır promptlardan birini seçebilir
        //
        public async Task<IActionResult> GenerateImage(string assetId)
        {
            var generateMediaResponse = await _renderNetGenerationService.GenerateMediaAsync(assetId);
            return Ok();
        }
    }
}
