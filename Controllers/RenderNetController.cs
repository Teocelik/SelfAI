using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SelfAI.BackgroundServices;
using SelfAI.DTOs.Legacy.RenderNet.Upload;
using SelfAI.DTOs.Legacy.RenderNet.Generation;
using SelfAI.Services.Interfaces;
using SelfAI.Services.Generation.Providers.Legacy.Affogato;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Orchestrators;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace SelfAI.Controllers
{
    [Authorize]
    public class RenderNetController : Controller
    {
        private readonly IRenderNetAssetService _renderNetAssetService;
        private readonly IRenderNetGenerationService _renderNetGenerationService;
        private readonly IRenderNetResourcesService _renderNetResourcesService;
        private readonly IRenderNetCharacterService _renderNetCharacterService;
        private readonly ICreditService _creditService;
        private readonly IGenerationLogService _generationLogService;
        private readonly ILogger<RenderNetController> _logger;
        private readonly GenerationPollingService _pollingService;
        private readonly IGenerationOrchestrator _orchestrator;

        public RenderNetController(IRenderNetAssetService renderNetAssetService, IRenderNetGenerationService renderNetGenerationService, IRenderNetResourcesService renderNetResourcesService, IRenderNetCharacterService renderNetCharacterService, ICreditService creditService, IGenerationLogService generationLogService, ILogger<RenderNetController> logger, GenerationPollingService pollingService, IGenerationOrchestrator orchestrator)
        {
            _renderNetAssetService = renderNetAssetService;
            _renderNetGenerationService = renderNetGenerationService;
            _renderNetResourcesService = renderNetResourcesService;
            _renderNetCharacterService = renderNetCharacterService;
            _creditService = creditService;
            _generationLogService = generationLogService;
            _logger = logger;
            _pollingService = pollingService;
            _orchestrator = orchestrator;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            _logger.LogInformation("Ana sayfa yüklendi!");
            return View();
        }

        // Görsel oluşturma isteği — fal.ai routing (F.M.3). Controller sadece HTTP
        // transport: auth claim'leri çözer, connectionId'yi okur, orchestrator'a delege eder.
        [HttpPost]
        public async Task<IActionResult> GenerateImage([FromBody] StartGenerationRequest dto)
        {
            // Auth — Firebase UID (SignalR routing fallback) + AppUser.Id (DB / cüzdan)
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var appUserIdStr = User.FindFirst("AppUserId")?.Value;

            if (string.IsNullOrEmpty(firebaseUid) || !Guid.TryParse(appUserIdStr, out var appUserId))
            {
                _logger.LogWarning("GenerateImage: Authenticated kullanıcı bulunamadı veya AppUserId claim eksik.");
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
            }

            // SignalR connectionId — primary push hedefi (orchestrator yoksa firebaseUid'e fallback yapar)
            var connectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();

            var result = await _orchestrator.StartGenerationAsync(dto, appUserId, firebaseUid, connectionId);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, data = result.Data, message = result.Message });
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


        // Stilleri çekmek için gerekli action metot
        [HttpGet]
        public async Task<IActionResult> GetStyles(string type = "flux")
        {
            var result = await _renderNetResourcesService.GetStylesAsync(type);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        // Flux modellerini çekmek için gerekli action metot
        [HttpGet]
        public async Task<IActionResult> GetModels(string type = "flux")
        {
            var result = await _renderNetResourcesService.GetModelsAsync(type);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        // Karakterleri çekmek için gerekli action metot
        [HttpGet]
        public async Task<IActionResult> GetCharacters(int page = 1, int pageSize = 50)
        {
            var result = await _renderNetCharacterService.GetCharactersAsync(page, pageSize);

            if (result.IsSuccess)
                return Ok(new { success = true, message = result.Message, data = result.Data });

            return StatusCode(result.StatusCode, new { success = false, message = result.Message });
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
