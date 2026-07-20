using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.Music;
using SelfAI.Services.Interfaces;
using SelfAI.ViewModels.Music;
using System.Security.Claims;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Music sekmesi (/Studio/Music) — F.M.10c. Backing track (Sonilo) + Full Song (MiniMax) +
/// otomatik albüm kapağı (Ideogram V3). Controller yalnızca HTTP transport: claim çözümü,
/// header okuma, ModelState validation, ServiceResult → HTTP dönüşümü. İş mantığı orchestrator'da.
/// </summary>
[Authorize]
[Route("Studio/Music")]
public class MusicStudioController : Controller
{
    private readonly IMusicGenerationOrchestrator _orchestrator;
    private readonly IContentModerationService _moderationService;
    private readonly ILogger<MusicStudioController> _logger;

    public MusicStudioController(
        IMusicGenerationOrchestrator orchestrator,
        IContentModerationService moderationService,
        ILogger<MusicStudioController> logger)
    {
        _orchestrator = orchestrator;
        _moderationService = moderationService;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        var vm = new MusicStudioIndexViewModel
        {
            BackingDurations = new[] { 15, 30, 60 },
            SongDurations = new[] { 60, 90, 180 },
            CoverAspectRatios = new[]
            {
                new CoverAspectOption { Value = "1:1", Label = "Kare (Spotify, Apple Music)", Icon = "square" },
                new CoverAspectOption { Value = "9:16", Label = "Dikey (Story, TikTok)", Icon = "portrait" },
                new CoverAspectOption { Value = "16:9", Label = "Yatay (YouTube)", Icon = "landscape" },
                new CoverAspectOption { Value = "4:5", Label = "Instagram Post", Icon = "portrait-narrow" }
            }
        };

        return View("~/Views/Studio/Music.cshtml", vm);
    }

    [HttpPost("Generate")]
    public async Task<IActionResult> Generate([FromBody] MusicGenerationRequest dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { message = string.Join(" ", errors) });
        }

        if (!TryResolveUser(out var appUserId, out var firebaseUid))
        {
            _logger.LogWarning("Music Generate: kimlik doğrulanamadı veya AppUserId claim eksik.");
            return Unauthorized(new { message = "Oturum bulunamadı." });
        }

        // F.8 — Content moderation (kredi düşme ÖNCESİ). Music prompt + lyrics ayrı denetlenir.
        var promptCheck = _moderationService.CheckPrompt(dto.MusicPrompt);
        if (promptCheck.IsBlocked)
        {
            _logger.LogWarning(
                "Music prompt moderation ile bloklandı. | UserId: {UserId} | Category: {Category}",
                appUserId, promptCheck.Category);
            return BadRequest(new { message = promptCheck.UserMessage, category = promptCheck.Category });
        }

        if (!string.IsNullOrWhiteSpace(dto.Lyrics))
        {
            var lyricsCheck = _moderationService.CheckPrompt(dto.Lyrics);
            if (lyricsCheck.IsBlocked)
            {
                _logger.LogWarning(
                    "Music lyrics moderation ile bloklandı. | UserId: {UserId} | Category: {Category}",
                    appUserId, lyricsCheck.Category);
                return BadRequest(new
                {
                    message = "Şarkı sözlerinde sorunlu içerik: " + lyricsCheck.UserMessage,
                    category = lyricsCheck.Category
                });
            }
        }

        // SignalR connectionId header'dan (Templates/Image ile aynı pattern) — progress push için.
        dto.SignalRConnectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        var result = await _orchestrator.StartGenerationAsync(dto, appUserId, firebaseUid, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Message });

        return Ok(result.Data);
    }

    // Firebase UID (NameIdentifier) + AppUserId claim çözümü (mevcut Studio pattern'i, IUserContextService YOK).
    private bool TryResolveUser(out Guid appUserId, out string firebaseUid)
    {
        appUserId = Guid.Empty;
        firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var appUserIdStr = User.FindFirst("AppUserId")?.Value;
        return !string.IsNullOrEmpty(firebaseUid) && Guid.TryParse(appUserIdStr, out appUserId);
    }
}
