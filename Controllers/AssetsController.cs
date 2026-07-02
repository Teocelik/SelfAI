using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.Entities.Enums;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    /// <summary>
    /// Asset upload/list/delete endpoint'i (F.M.7). Face Lock / Pose Lock / generic upload için
    /// tek dosyalık akış. Character training upload'ları buradan YASAKLI — /Characters/Create kullanır.
    ///
    /// HTTP transport only — iş mantığı yok. Upload + persist + resolve IAssetService'te.
    /// </summary>
    [ApiController]
    [Route("Assets")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetsController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        /// <summary>
        /// Face Lock / Pose Lock / generic tek dosya upload. Purpose query parametresiyle amaç belirtilir.
        /// </summary>
        [HttpPost("Upload")]
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromQuery] string purpose,
            CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });

            // Purpose parse — kullanıcı input, güvenli parse
            if (!Enum.TryParse<AssetPurpose>(purpose, ignoreCase: true, out var purposeEnum))
                return BadRequest(new { success = false, message = "Geçersiz purpose. FaceLock, PoseLock veya Generic olmalı." });

            // Character training upload'ı bu endpoint'ten YASAKLI — CharactersController.Create kullanmalı
            if (purposeEnum == AssetPurpose.CharacterTraining)
                return BadRequest(new
                {
                    success = false,
                    message = "Karakter training upload'ları /Characters/Create endpoint'inden yapılır."
                });

            var result = await _assetService.UploadAsync(file, userId, purposeEnum, ct);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, data = result.Data, message = result.Message });
        }

        /// <summary>
        /// Kullanıcının kendi asset'lerini listeler (opsiyonel purpose filtresi).
        /// </summary>
        [HttpGet("List")]
        public async Task<IActionResult> List(
            [FromQuery] string? purpose,
            CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });

            AssetPurpose? purposeFilter = null;
            if (!string.IsNullOrEmpty(purpose) &&
                Enum.TryParse<AssetPurpose>(purpose, ignoreCase: true, out var parsed))
                purposeFilter = parsed;

            var result = await _assetService.ListUserAssetsAsync(userId, purposeFilter, ct);
            return Ok(new { success = true, data = result.Data });
        }

        /// <summary>Asset silme (soft delete).</summary>
        [HttpDelete("{assetId:guid}")]
        public async Task<IActionResult> Delete(Guid assetId, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });

            var result = await _assetService.DeleteAsync(assetId, userId, ct);

            if (!result.IsSuccess)
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        // AppUser.Id claim'i (DB/cüzdan). RenderNetController + CharactersController ile aynı pattern.
        private bool TryGetUserId(out Guid userId)
        {
            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            return Guid.TryParse(appUserIdStr, out userId);
        }
    }
}
