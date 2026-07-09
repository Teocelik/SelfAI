using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.BackgroundServices;
using SelfAI.Services.Interfaces;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Orchestrators;
using System.Security.Claims;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Görsel üretimi Studio sekmesi (/Studio/Image). F.M.Arch.1'de RenderNetController'dan
/// taşındı — DAVRANIŞ BİREBİR AYNI, sadece route + view konumu değişti. Görsel üretim,
/// Face Lock, Character LoRA, dynamic model akışları buradan yürür.
/// </summary>
[Authorize]
[Route("Studio/Image")]
public class ImageStudioController : Controller
{
    private readonly ICreditService _creditService;
    private readonly IGenerationLogService _generationLogService;
    private readonly ILogger<ImageStudioController> _logger;
    private readonly GenerationPollingService _pollingService;
    private readonly IGenerationOrchestrator _orchestrator;

    public ImageStudioController(ICreditService creditService, IGenerationLogService generationLogService, ILogger<ImageStudioController> logger, GenerationPollingService pollingService, IGenerationOrchestrator orchestrator)
    {
        _creditService = creditService;
        _generationLogService = generationLogService;
        _logger = logger;
        _pollingService = pollingService;
        _orchestrator = orchestrator;
    }

    [AllowAnonymous]
    [HttpGet("")]
    public IActionResult Index()
    {
        _logger.LogInformation("Ana sayfa yüklendi!");
        return View("~/Views/Studio/Image.cshtml");
    }

    // Görsel oluşturma isteği — fal.ai routing (F.M.3). Controller sadece HTTP
    // transport: auth claim'leri çözer, connectionId'yi okur, orchestrator'a delege eder.
    [HttpPost("GenerateImage")]
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
