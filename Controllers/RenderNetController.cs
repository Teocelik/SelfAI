using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SelfAI.BackgroundServices;
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
        private readonly GenerationPollingService _pollingService;

        public RenderNetController(IRenderNetAssetService renderNetAssetService, IRenderNetGenerationService renderNetGenerationService, IRenderNetResourcesService renderNetResourcesService, ILogger<RenderNetController> logger, GenerationPollingService pollingService)
        {
            _renderNetAssetService = renderNetAssetService;
            _renderNetGenerationService = renderNetGenerationService;
            _renderNetResourcesService = renderNetResourcesService;
            _logger = logger;
            _pollingService = pollingService;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Ana sayfa yüklendi!");
            return View();
        }

        //Görsel oluşturma isteği için gerekli action metot
        [HttpPost]
        public async Task<IActionResult> GenerateImage(
    MediaGenerationRequestDto requestDto,
    [FromHeader(Name = "X-SignalR-ConnectionId")] string connectionId,
    [FromHeader(Name = "X-Client-Id")] string clientId)  
        {
            var result = await _renderNetGenerationService.GenerateMediaAsync(requestDto);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            var generationId = result.Data.Data.GenerationId;

            // 🆕 clientId + connectionId birlikte gönderiliyor
            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(connectionId))
            {
                _pollingService.AddJob(generationId, clientId, connectionId);
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                generationId = generationId
            });
        }



        /// <summary>
        /// 🆕 Generation durumunu kontrol et (Polling endpoint)
        /// Frontend belirli aralıklarla bu endpoint'i çağırır
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetGenerationStatus(string generationId)
        {
            if (string.IsNullOrEmpty(generationId))
                return BadRequest(new { success = false, message = "Generation ID gerekli." });

            var result = await _renderNetGenerationService.GetGenerationAsync(generationId);

            if (result.IsSuccess)
            {
                var media = result.Data.Data.Media;

                // Tüm medyaların durumunu kontrol et
                bool allCompleted = media.All(m => m.Status == "success");
                bool anyFailed = media.Any(m => m.Status == "failed");

                return Ok(new
                {
                    success = true,
                    completed = allCompleted,       // Tümü hazır mı?
                    failed = anyFailed,             // Herhangi biri başarısız mı?
                    media = media.Select(m => new   // Her medyanın durumu
                    {
                        id = m.Id,
                        status = m.Status,
                        url = m.Url,                // success ise URL dolu
                        type = m.Type
                    })
                });
            }

            return StatusCode(result.StatusCode, new { success = false, message = result.Message });
        }


        // Flux image style'lerini çekmek için gerekli action metot
        [HttpGet]
        public async Task<IActionResult> GetFluxStyles()
        {
            try
            {
                var fluxStyleResult = await _renderNetResourcesService.GetFluxStylesAsync();

                return Ok(new { data = fluxStyleResult.Data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
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
                    assetId = uploadImageResponse.Data?.Data.Asset.Id,
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
