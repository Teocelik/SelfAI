using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SelfAI.DTOs.Templates;
using SelfAI.Entities.Enums;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Orchestrators;
using SelfAI.Services.Interfaces;
using SelfAI.ViewModels.Templates;
using System.Security.Claims;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Post Templates sekmesi (/Studio/Templates) — F.M.10a (format-first) + F.M.10b (hazır şablon +
/// text overlay + karakter LoRA). Controller yalnızca HTTP transport: claim çözümü, header okuma,
/// ModelState validation, ServiceResult → HTTP dönüşümü.
///
/// Üç üretim akışı (<see cref="Generate"/>): PresetId dolu → preset + overlay; FormatId dolu →
/// format-first; ikisi de opsiyonel CharacterId ile flux-lora'ya route edilir (override'ı
/// <see cref="IGenerationOrchestrator"/> CharacterId'den kendisi yapar — controller/builder
/// endpoint'e dokunmaz). Kredi/log/SignalR mevcut orchestrator pipeline'ında (ayrı orchestrator YOK).
/// </summary>
[Authorize]
[Route("Studio/Templates")]
public class TemplatesStudioController : Controller
{
    private readonly ITemplateCatalogService _catalog;
    private readonly IPresetTemplateCatalogService _presetCatalog;
    private readonly IGenerationOrchestrator _generationOrchestrator;
    private readonly ICharacterService _characterService;
    private readonly ITextOverlayService _textOverlayService;
    private readonly IAssetService _assetService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TemplatesStudioController> _logger;

    private const string PresetPendingCacheKeyPrefix = "preset-pending:";

    public TemplatesStudioController(
        ITemplateCatalogService catalog,
        IPresetTemplateCatalogService presetCatalog,
        IGenerationOrchestrator generationOrchestrator,
        ICharacterService characterService,
        ITextOverlayService textOverlayService,
        IAssetService assetService,
        IMemoryCache memoryCache,
        ILogger<TemplatesStudioController> logger)
    {
        _catalog = catalog;
        _presetCatalog = presetCatalog;
        _generationOrchestrator = generationOrchestrator;
        _characterService = characterService;
        _textOverlayService = textOverlayService;
        _assetService = assetService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
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
                .ToList(),
            Presets = _presetCatalog.GetAllPresets()
                .Select(p => new PresetTemplateViewModel
                {
                    Id = p.Id,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Platform = p.Platform,
                    Width = p.Width,
                    Height = p.Height,
                    PreviewImageUrl = p.PreviewImageUrl,
                    TextFields = p.TextFields
                        .Select(tf => new PresetTextFieldViewModel
                        {
                            Id = tf.Id,
                            Label = tf.Label,
                            Placeholder = tf.Placeholder,
                            MaxLength = tf.MaxLength
                        })
                        .ToList()
                })
                .ToList(),
            UserCharacters = await GetReadyCharactersAsync(ct)
        };

        _logger.LogInformation(
            "Templates sayfası yüklendi! | FormatCount: {FC} | PresetCount: {PC} | CharCount: {CC}",
            vm.Formats.Count, vm.Presets.Count, vm.UserCharacters.Count);
        return View("~/Views/Studio/Templates.cshtml", vm);
    }

    // Üretim başlatma — F.M.10a format-first VEYA F.M.10b preset akışı. SignalR connectionId
    // X-SignalR-ConnectionId header'ından; sonuç fire-and-forget (frontend SignalR bekler).
    [HttpPost("Generate")]
    public async Task<IActionResult> Generate([FromBody] TemplateGenerationRequest dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        if (!TryResolveUser(out var appUserId, out var firebaseUid))
        {
            _logger.LogWarning("Templates Generate: kimlik doğrulanamadı veya AppUserId claim eksik.");
            return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
        }

        var connectionId = Request.Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        // Akış seçimi: PresetId öncelikli → preset + overlay; aksi halde FormatId → format-first.
        if (!string.IsNullOrEmpty(dto.PresetId))
            return await GenerateFromPresetAsync(dto, appUserId, firebaseUid, connectionId, ct);

        if (!string.IsNullOrEmpty(dto.FormatId))
            return await GenerateFromFormatAsync(dto, appUserId, firebaseUid, connectionId, ct);

        return BadRequest(new { success = false, message = "FormatId veya PresetId belirtilmeli." });
    }

    private async Task<IActionResult> GenerateFromFormatAsync(
        TemplateGenerationRequest dto, Guid appUserId, string firebaseUid, string? connectionId, CancellationToken ct)
    {
        var format = _catalog.GetFormat(dto.FormatId!);
        if (format == null)
        {
            _logger.LogWarning("Templates Generate: geçersiz format. | FormatId: {FormatId}", dto.FormatId);
            return BadRequest(new { success = false, message = "Geçersiz format." });
        }

        // Karakter override'ını orchestrator CharacterId'den yapar — builder yalnızca taşır.
        var startRequest = TemplateStartRequestBuilder.Build(format, dto);

        _logger.LogInformation(
            "Template (format) generation. | UserId: {UserId} | Format: {FormatId} | Char: {Char} | Aspect: {Aspect}",
            appUserId, format.Id, dto.CharacterId, format.AspectRatioValue);

        var result = await _generationOrchestrator.StartGenerationAsync(
            startRequest, appUserId, firebaseUid, connectionId, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Data, message = result.Message });
    }

    private async Task<IActionResult> GenerateFromPresetAsync(
        TemplateGenerationRequest dto, Guid appUserId, string firebaseUid, string? connectionId, CancellationToken ct)
    {
        var preset = _presetCatalog.GetPreset(dto.PresetId!);
        if (preset == null)
        {
            _logger.LogWarning("Templates Generate: geçersiz preset. | PresetId: {PresetId}", dto.PresetId);
            return BadRequest(new { success = false, message = "Geçersiz preset." });
        }

        var startRequest = PresetStartRequestBuilder.Build(preset, dto);

        _logger.LogInformation(
            "Template (preset) generation. | UserId: {UserId} | Preset: {PresetId} | Char: {Char} | Aspect: {Aspect}",
            appUserId, preset.Id, dto.CharacterId, preset.AspectRatioValue);

        var result = await _generationOrchestrator.StartGenerationAsync(
            startRequest, appUserId, firebaseUid, connectionId, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { success = false, message = result.Message });

        // Text overlay için pending veriyi cache'e koy (post-generation'da PostProcess çeker).
        // 1 saat TTL — generation genelde 30-120 sn. Kredi/generation zaten commit oldu.
        var pending = new PendingPresetPostProcessing
        {
            PresetId = preset.Id,
            TextValues = dto.PresetTextValues ?? new Dictionary<string, string>(),
            UserId = appUserId
        };
        _memoryCache.Set(PresetPendingCacheKeyPrefix + result.Data!.GenerationId, pending, TimeSpan.FromHours(1));

        return Ok(new { success = true, data = result.Data, isPreset = true, message = result.Message });
    }

    // Preset generation tamamlandıktan sonra text overlay render + R2 upload (F.M.10b, Yaklaşım C).
    // Stateless: frontend generation Complete'i SignalR'dan alır, ham URL ile bu endpoint'i çağırır.
    [HttpPost("PostProcess")]
    public async Task<IActionResult> PostProcess([FromBody] PresetPostProcessRequest dto, CancellationToken ct)
    {
        if (!TryResolveUser(out var appUserId, out _))
            return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });

        var cacheKey = PresetPendingCacheKeyPrefix + dto.GenerationId;
        if (!_memoryCache.TryGetValue(cacheKey, out PendingPresetPostProcessing? pending) || pending == null)
        {
            _logger.LogWarning("Preset PostProcess: pending veri yok. | GenId: {Id}", dto.GenerationId);
            return BadRequest(new { success = false, message = "İşlenecek şablon verisi bulunamadı." });
        }

        if (pending.UserId != appUserId)
        {
            _logger.LogWarning(
                "Preset PostProcess: sahiplik uyuşmazlığı. | GenId: {Id} | Owner: {Owner} | Caller: {Caller}",
                dto.GenerationId, pending.UserId, appUserId);
            return StatusCode(403, new { success = false, message = "Bu işleme yetkiniz yok." });
        }

        var preset = _presetCatalog.GetPreset(pending.PresetId);
        if (preset == null)
            return BadRequest(new { success = false, message = "Şablon bulunamadı." });

        // Preset config (sabit spec) + kullanıcı metni → overlay spec listesi. Boş alanlar skip.
        var overlays = preset.TextFields
            .Where(tf => pending.TextValues.TryGetValue(tf.Id, out var val) && !string.IsNullOrWhiteSpace(val))
            .Select(tf =>
            {
                var s = tf.OverlaySpec;
                return new TextOverlaySpec
                {
                    Text = pending.TextValues[tf.Id],
                    X = s.X, Y = s.Y, MaxWidth = s.MaxWidth, FontSize = s.FontSize,
                    FontName = s.FontName, Color = s.Color, Alignment = s.Alignment, DropShadow = s.DropShadow
                };
            })
            .ToList();

        try
        {
            await using var overlayStream = await _textOverlayService.RenderAsync(dto.SourceImageUrl, overlays, ct);
            var sizeBytes = overlayStream.Length;

            var fileName = $"preset-{preset.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
            var uploadResult = await _assetService.UploadAsync(
                overlayStream, fileName, "image/png", sizeBytes, appUserId, AssetPurpose.Generic, ct);

            if (!uploadResult.IsSuccess || uploadResult.Data == null)
            {
                _logger.LogError("Preset PostProcess: overlay upload başarısız. | GenId: {Id}", dto.GenerationId);
                return StatusCode(502, new { success = false, message = "İşlenen görsel yüklenemedi." });
            }

            _memoryCache.Remove(cacheKey);

            _logger.LogInformation(
                "Preset PostProcess tamamlandı. | GenId: {Id} | Overlays: {Count} | AssetId: {AssetId}",
                dto.GenerationId, overlays.Count, uploadResult.Data.Id);

            return Ok(new
            {
                success = true,
                data = new { imageUrl = uploadResult.Data.Url, assetId = uploadResult.Data.Id }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preset PostProcess hatası. | GenId: {Id}", dto.GenerationId);
            return StatusCode(500, new { success = false, message = "Metin ekleme sırasında bir hata oluştu." });
        }
    }

    // ═══ Yardımcılar ═══

    // Firebase UID (NameIdentifier) + AppUserId claim çözümü (mevcut Studio pattern'i).
    private bool TryResolveUser(out Guid appUserId, out string firebaseUid)
    {
        appUserId = Guid.Empty;
        firebaseUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var appUserIdStr = User.FindFirst("AppUserId")?.Value;
        return !string.IsNullOrEmpty(firebaseUid) && Guid.TryParse(appUserIdStr, out appUserId);
    }

    // Kullanıcının yalnızca Ready durumundaki karakterlerini karakter seçici için döner.
    // GetReadyCharactersAsync YOK — mevcut GetUserCharactersAsync + TrainingStatus filtresi.
    private async Task<List<UserCharacterViewModel>> GetReadyCharactersAsync(CancellationToken ct)
    {
        if (!TryResolveUser(out var appUserId, out _))
            return new List<UserCharacterViewModel>();

        var result = await _characterService.GetUserCharactersAsync(appUserId, page: 1, pageSize: 50);
        if (!result.IsSuccess || result.Data == null)
            return new List<UserCharacterViewModel>();

        return result.Data.Items
            .Where(c => string.Equals(c.TrainingStatus, "Ready", StringComparison.OrdinalIgnoreCase)
                        && Guid.TryParse(c.Id, out _))
            .Select(c => new UserCharacterViewModel
            {
                Id = Guid.Parse(c.Id),
                Name = c.Name,
                ThumbnailUrl = string.IsNullOrEmpty(c.ThumbnailUrl) ? null : c.ThumbnailUrl
            })
            .ToList();
    }
}
