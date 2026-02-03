using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SelfAI.DTOs.RenderNet;
using SelfAI.DTOs.RenderNetGenerationRequestDtos;
using SelfAI.DTOs.RenderNetUploadResponseDtos;
using SelfAI.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SelfAI.Controllers
{
    public class RenderNetController : Controller
    {
        private readonly IRenderNetAssetService _renderNetAssetService;
        private readonly IRenderNetGenerationService _renderNetGenerationService;
        private readonly IRenderNetResourcesService _renderNetResourcesService;
        private readonly ILogger<RenderNetController> _logger;

        public RenderNetController(IRenderNetAssetService renderNetAssetService, IRenderNetGenerationService renderNetGenerationService, IRenderNetResourcesService renderNetResourcesService, ILogger<RenderNetController> logger)
        {
            _renderNetAssetService = renderNetAssetService;
            _renderNetGenerationService = renderNetGenerationService;
            _renderNetResourcesService = renderNetResourcesService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Görsel oluşturma isteği için gerekli action metot
        [HttpPost]
        public async Task<IActionResult> GenerateImage(MediaGenerationRequestDto requestDto)
        {
            // varlığı UploadUrl'ye yükleyelim(asset_id'yi bu servisten alıyoruz)
            //var uploadImageResponse = await _renderNetAssetService.UploadAssetAsync(requestDto);

            // media oluşturma isteğini yapalım
            var generateMediaResponse = await _renderNetGenerationService.GenerateMediaAsync(requestDto);

            // Eğer media oluşturma başarılı ise, sonucu döndürelim
            //Şimdilik sadece başarılı olduğunu kontrol ediyoruz
            return Ok(generateMediaResponse);
        }


        // Flux image style'lerini çekmek için gerekli action metot
        [HttpGet]
        public async Task<IActionResult> GetFluxStyles()
        {
            var fluxStyleList = await _renderNetResourcesService.GetFluxStylesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> GetAssetId(IFormFile formFile)
        {
            //if (formFile == null || formFile.Length == 0)
            //{
            //    return BadRequest(new { success = false, message = "Dosya yüklenmedi." });
            //}

            try
            {
                // Dosyayı okuyalım(veriye erişim vanasını açmak(musluk gibi düşün!))
                using var stream = formFile.OpenReadStream();

                var request = new UploadAssetRequestDto
                {
                    FileName = formFile.FileName,
                    ContentType = formFile.ContentType,
                    Content = stream,
                    // StreamContent için dosya boyutunu bilmek bazen gerekebilir
                    Length = formFile.Length
                };

                var uploadImageResponse = await _renderNetAssetService.GetAssetIdAsync(request);

                return Ok(new
                {
                    success = true,
                    assetId = uploadImageResponse.Data.Asset.Id,
                    data = uploadImageResponse
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
