using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SelfAI.DTOs.Characters;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    // Karakter yönetimi endpoint'leri (F.6.1 + F.M.4). Mevcut MVC konvansiyonu: Controller + default route.
    [Authorize]
    public class CharactersController : Controller
    {
        private readonly ICharacterService _service;
        private readonly ICharacterTrainingOrchestrator _trainingOrchestrator;
        private readonly ILogger<CharactersController> _logger;

        public CharactersController(
            ICharacterService service,
            ICharacterTrainingOrchestrator trainingOrchestrator,
            ILogger<CharactersController> logger)
        {
            _service = service;
            _trainingOrchestrator = trainingOrchestrator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> List(int page = 1, int pageSize = 20)
        {
            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            if (!Guid.TryParse(appUserIdStr, out var appUserId))
            {
                _logger.LogWarning("Characters/List: AppUserId claim eksik.");
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
            }

            var result = await _service.GetUserCharactersAsync(appUserId, page, pageSize);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }

        // F.M.4 — fal.ai LoRA training ile karakter oluşturma. Multipart (yüz görselleri
        // doğrudan dosya olarak gelir, önceden asset upload YOK). [FromForm] zorunlu.
        // Çoklu görsel tek istekte geldiği için Kestrel/multipart varsayılan limitleri
        // yükseltilir (10 görsel × 15MB + overhead ≈ 200MB).
        [HttpPost]
        [RequestSizeLimit(210_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 210_000_000)]
        public async Task<IActionResult> Create([FromForm] CharacterCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            // SignalR routing için Firebase UID + DB/cüzdan için AppUser.Id
            var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var appUserIdStr = User.FindFirst("AppUserId")?.Value;

            if (string.IsNullOrEmpty(firebaseUid) || !Guid.TryParse(appUserIdStr, out var appUserId))
            {
                _logger.LogWarning("Characters/Create: kimlik veya AppUserId claim eksik.");
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
            }

            var result = await _trainingOrchestrator.StartTrainingAsync(request, appUserId, firebaseUid);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = new { characterId = result.Data } });
        }

        [HttpPost]
        public async Task<IActionResult> Archive([FromBody] CharacterArchiveRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            if (!Guid.TryParse(appUserIdStr, out var appUserId))
            {
                _logger.LogWarning("Characters/Archive: AppUserId claim eksik.");
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
            }

            var result = await _service.ArchiveCharacterAsync(appUserId, request.CharacterId);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }
    }
}
