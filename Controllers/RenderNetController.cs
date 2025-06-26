using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SelfAI.Services.Interfaces;
using System.Net.Http.Headers;

namespace SelfAI.Controllers
{
    public class RenderNetController : Controller
    {
        private readonly IRenderNetAssetService _renderNetAssetService;
        
        public RenderNetController(IRenderNetAssetService renderNetAssetService)
        {
            _renderNetAssetService = renderNetAssetService;
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

            // Yükleme başarılı ise, assetId'yi döndürelim
            return Ok();
        }
    }
}