using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.Templates;
using SelfAI.Services.Interfaces;
using SelfAI.ViewModels.Templates;
using System.Security.Claims;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Post Templates sekmesi (/Studio/Templates) — F.M.10a. Format-first sosyal medya
/// üretimi. Controller yalnızca HTTP transport: claim çözümü, header okuma, ModelState
/// validation, ServiceResult → HTTP dönüşümü. İş mantığı orchestrator'da.
/// </summary>
[Authorize]
[Route("Studio/Templates")]
public class TemplatesStudioController : Controller
{
    private readonly ITemplateCatalogService _catalog;
    private readonly ITemplateGenerationOrchestrator _orchestrator;
    private readonly ILogger<TemplatesStudioController> _logger;

    public TemplatesStudioController(
        ITemplateCatalogService catalog,
        ITemplateGenerationOrchestrator orchestrator,
        ILogger<TemplatesStudioController> logger)
    {
        _catalog = catalog;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var vm = new TemplatesIndexViewModel
        {
            Formats = _catalog.GetAllFormats()
                .Select(f => new PostFormatViewModel
                {
                    Id = f.Id,
                    DisplayName = f.DisplayName,
                    Description = f.Description,
                    Platform = f.Platform,
                    Width = f.Width,
                    Height = f.Height,
                    AspectRatioValue = f.AspectRatioValue,
                    PromptSuffix = f.PromptSuffix
                })
                .ToList()
        };

        _logger.LogInformation("Templates sayfası yüklendi! | FormatCount: {Count}", vm.Formats.Count);
        return View("~/Views/Studio/Templates.cshtml", vm);
    }

    // Üretim başlatma — mevcut Studio/Image ile aynı transport pattern:
    // Firebase UID + AppUserId claim'leri + X-SignalR-ConnectionId header. Sonuç
    // fire-and-forget; frontend SignalR "GenerationUpdate" bekler.
    [HttpPost("Generate")]
    public async Task<IActionResult> Generate([FromBody] TemplateGenerationRequest dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        var firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var appUserIdStr = User.FindFirst("AppUserId")?.Value;

        if (string.IsNullOrEmpty(firebaseUid) || !Guid.TryParse(appUserIdStr, out var appUserId))
        {
            _logger.LogWarning("Templates Generate: Authenticated kullanıcı bulunamadı veya AppUserId claim eksik.");
            return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
        }

        var connectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        var result = await _orchestrator.GenerateAsync(dto, appUserId, firebaseUid, connectionId, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Data, message = result.Message });
    }
}
