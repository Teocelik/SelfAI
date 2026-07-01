using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.Services.Generation.Abstractions;

namespace SelfAI.Controllers
{
    /// <summary>
    /// Face Lock / Pose Lock referans görseli upload endpoint'i (F.M.6).
    /// Kullanıcı tek bir görsel yükler → fal.ai storage'a gider → public URL döner;
    /// frontend bu URL'i generation request'inde faceImageUrl/poseImageUrl olarak yollar.
    ///
    /// HTTP transport only — iş mantığı yok. Storage iletişimi IFalAiStorageClient'ta.
    /// F.M.7'de Asset entity + library + S3 migration ile tam refactor edilecek.
    /// </summary>
    [ApiController]
    [Route("Assets")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private const long MaxImageSizeBytes = 10 * 1024 * 1024;  // 10MB
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

        private readonly IFalAiStorageClient _storageClient;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(IFalAiStorageClient storageClient, ILogger<AssetsController> logger)
        {
            _storageClient = storageClient;
            _logger = logger;
        }

        /// <summary>
        /// Face Lock veya Pose Lock için tek görsel upload eder, fal.ai storage URL'i döner.
        /// </summary>
        [HttpPost("UploadReference")]
        public async Task<IActionResult> UploadReference(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Dosya boş olamaz." });

            if (file.Length > MaxImageSizeBytes)
                return BadRequest(new { success = false, message = "Dosya boyutu en fazla 10MB olmalı." });

            if (!AllowedContentTypes.Contains(file.ContentType))
                return BadRequest(new { success = false, message = "Sadece JPEG, PNG veya WebP kabul edilir." });

            try
            {
                using var stream = file.OpenReadStream();
                var url = await _storageClient.UploadAsync(stream, file.FileName, file.ContentType, ct);

                _logger.LogInformation(
                    "Reference asset upload. | FileName: {File} | Size: {Size} | Url: {Url}",
                    file.FileName, file.Length, url);

                return Ok(new { success = true, data = new { url } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reference asset upload hatası. | FileName: {File}", file.FileName);
                return StatusCode(500, new { success = false, message = "Görsel yüklenemedi." });
            }
        }
    }
}
