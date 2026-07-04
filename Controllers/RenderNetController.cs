using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.BackgroundServices;
using SelfAI.Services.Interfaces;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Orchestrators;
using System.Security.Claims;

namespace SelfAI.Controllers
{
    [Authorize]
    public class RenderNetController : Controller
    {
        private readonly ICreditService _creditService;
        private readonly IGenerationLogService _generationLogService;
        private readonly ILogger<RenderNetController> _logger;
        private readonly GenerationPollingService _pollingService;
        private readonly IGenerationOrchestrator _orchestrator;

        public RenderNetController(ICreditService creditService, IGenerationLogService generationLogService, ILogger<RenderNetController> logger, GenerationPollingService pollingService, IGenerationOrchestrator orchestrator)
        {
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
    }
}
