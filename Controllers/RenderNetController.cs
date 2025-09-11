using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SelfAI.DTOs.RenderNet;
using SelfAI.DTOs.RenderNetGenerationRequestDtos;
using SelfAI.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SelfAI.Controllers
{
    public class RenderNetController : Controller
    {
        private readonly IRenderNetAssetService _renderNetAssetService;
        private readonly IRenderNetGenerationService _renderNetGenerationService;
        private readonly ILogger<RenderNetController> _logger;

        public RenderNetController(IRenderNetAssetService renderNetAssetService, IRenderNetGenerationService renderNetGenerationService, ILogger<RenderNetController> logger)
        {
            _renderNetAssetService = renderNetAssetService;
            _renderNetGenerationService = renderNetGenerationService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> GenerateImage(MediaGenerationRequestDto requestDto)
        {
            //// uploadUrl alalım
            //var uploadUrlResponse = await _renderNetAssetService.GetUploadUrlAsync();

            // varlığı UploadUrl'ye yükleyelim(asset_id'yi bu servisten alıyoruz)
            var uploadImageResponse = await _renderNetAssetService.UploadAssetAsync(requestDto);

            // media oluşturma isteğini yapalım
            var generateMediaResponse = await _renderNetGenerationService.GenerateMediaAsync(uploadImageResponse.Data.Asset.Id, requestDto);

            // Eğer media oluşturma başarılı ise, sonucu döndürelim
            //Şimdilik sadece başarılı olduğunu kontrol ediyoruz
            return Ok(generateMediaResponse);
        }


        //// UploadUrl'e görsel yüklemek için gerekli metot
        //[HttpPost]
        //public async Task<IActionResult> 5(MediaGenerationRequestDto requestDto)
        //{
        //    // uploadUrl alalım
        //    var uploadUrlResponse = await _renderNetAssetService.GetUploadUrlAsync();

        //    // varlığı UploadUrl'ye yükleyelim
        //    var uploadImageResponse = await _renderNetAssetService.UploadAssetAsync(requestDto);

        //    // Yükleme başarılı ise, GenerateImage metodunu çağırabiliriz

        //    return RedirectToAction("GenerateImage", new { assetId = uploadImageResponse.Data.Asset.Id });
        //}

        ////Kullanıcının görsel oluşturması için..
        //// kullanıcı promptu veya hazır promptlardan birini seçebilir
        //[HttpPost]
        //public async Task<IActionResult> GenerateImage(string assetId, MediaGenerationRequestDto requestDto)
        //{
        //    var generateMediaResponse = await _renderNetGenerationService.GenerateMediaAsync(assetId, requestDto);
        //    return Ok();
        //}
    }
}
