using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.Characters;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    // Karakter yönetimi endpoint'leri (F.6.1). Mevcut MVC konvansiyonu: Controller + default route.
    [Authorize]
    public class CharactersController : Controller
    {
        private readonly ICharacterService _service;
        private readonly ILogger<CharactersController> _logger;

        public CharactersController(
            ICharacterService service,
            ILogger<CharactersController> logger)
        {
            _service = service;
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CharacterCreateRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            if (!Guid.TryParse(appUserIdStr, out var appUserId))
            {
                _logger.LogWarning("Characters/Create: AppUserId claim eksik.");
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
            }

            var result = await _service.CreateCharacterAsync(appUserId, request);
            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message, data = result.Data });
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
