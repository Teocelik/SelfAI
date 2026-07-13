using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.Catalog;
using SelfAI.Services.Generation.Orchestrators;

namespace SelfAI.Controllers;

/// <summary>
/// Model catalog HTTP transport (F.M.5). Sadece auth claim çözümü + orchestrator
/// delegasyonu + ServiceResult → HTTP dönüşümü. İş mantığı yok.
/// </summary>
[ApiController]
[Route("Catalog")]
[Authorize]
public class CatalogController : ControllerBase
{
    private readonly IModelCatalogOrchestrator _orchestrator;

    public CatalogController(IModelCatalogOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpGet("Image")]
    public async Task<IActionResult> GetImageCatalog(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _orchestrator.GetCatalogAsync("text-to-image", userId, ct);

        return result.IsSuccess
            ? Ok(new { success = true, data = result.Data })
            : StatusCode(result.StatusCode, new { success = false, message = result.Message });
    }

    [HttpPost("ToggleFavorite")]
    public async Task<IActionResult> ToggleFavorite(
        [FromBody] ToggleFavoriteRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });

        if (request == null || string.IsNullOrWhiteSpace(request.EndpointId))
            return BadRequest(new { success = false, message = "Endpoint ID gerekli." });

        var result = await _orchestrator.ToggleFavoriteAsync(userId.Value, request.EndpointId, ct);

        return result.IsSuccess
            ? Ok(new { success = true, isFavorited = result.Data })
            : StatusCode(result.StatusCode, new { success = false, message = result.Message });
    }

    /// <summary>
    /// fal.ai'dan model listesini çekip ModelCatalogEntry metadata'sını (display name,
    /// description, thumbnail) günceller; yeni modelleri Pending ekler. Status/Tier/CostUsd
    /// admin override'ı korunur. Manuel tetiklenir.
    /// NOT: Şimdilik authenticated user yeterli; F.9 Admin paneli gelince
    /// [Authorize(Roles="Admin")] eklenecek.
    /// </summary>
    [HttpPost("Sync")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        // F.9b — Sync imzasına category parametresi eklendi; davranış birebir korunur
        // (default "text-to-image"). ct named arg ile geçilir.
        var result = await _orchestrator.SyncFromFalAiAsync(cancellationToken: ct);

        return result.IsSuccess
            ? Ok(new { success = true, message = result.Message, data = result.Data })
            : StatusCode(result.StatusCode, new { success = false, message = result.Message });
    }

    /// <summary>AppUserId claim'inden domain user Guid'ini çözer (yoksa null).</summary>
    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirst("AppUserId")?.Value;
        return Guid.TryParse(raw, out var id) ? id : (Guid?)null;
    }
}
