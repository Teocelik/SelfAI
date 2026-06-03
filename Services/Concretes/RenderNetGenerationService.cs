using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.RenderNetGenerationRequestDtos;
using SelfAI.DTOs.RenderNetGenerationResponseDtos; // 🆕
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfAI.Services.Concretes
{
    public class RenderNetGenerationService : IRenderNetGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetOptions _settings;
        private readonly ILogger<RenderNetGenerationService> _logger;

        // Pose Lock (ControlNet) sabitleri.
        // NOT: "Openpose" placeholder, SD ekosistem standardına göre konuldu.
        // Paid API hesabı alındığında GET /pub/v1/controlnets çağrılıp
        // RenderNet'in pose ControlNet adı doğrulanmalı, eşleşmiyorsa burası
        // güncellenmeli.
        private const string POSE_NAME = "Openpose";
        // control_mode: API integer enum (0=Balanced, 1=Prompt öncelikli, 2=ControlNet öncelikli).
        private const int POSE_CONTROL_MODE = 0;
        // resize_mode: API integer enum (0=Resize&Fill, 1=Crop&Resize, 2=Just Resize).
        private const int POSE_RESIZE_MODE = 0;

        // JSON ayarları (snake_case API yanıtları için)
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Request payload için: null alanları (örn. opsiyonel facelock) serialize etme.
        private readonly JsonSerializerOptions _requestJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public RenderNetGenerationService(
            HttpClient httpClient,
            IOptions<RenderNetOptions> options,
            ILogger<RenderNetGenerationService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        /// <summary>
        /// Görsel oluşturma isteği
        /// generation_id parse ediliyor
        /// </summary>
        public async Task<ServiceResult<GenerateMediaResponseDto>> GenerateMediaAsync(MediaGenerationRequestDto dto)
        {
            // Multi-model desteği: Model ve Style alanları virgülle ayrılmış birden fazla
            // değer içerebilir (örn. "Flux,JuggernautXL" / "Cinematic,Anime").
            // Bunlar listelere parse edilip her biri için ayrı bir generation elemanı kurulur.
            var models = string.IsNullOrWhiteSpace(dto.Model)
                ? new List<string>()
                : dto.Model.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s))
                           .ToList();

            var styles = string.IsNullOrWhiteSpace(dto.Style)
                ? new List<string>()
                : dto.Style.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrEmpty(s))
                           .ToList();

            // Count validation: her iki liste de doluysa sayıları eşleşmeli (1-1 pair).
            if (models.Count > 0 && styles.Count > 0 && models.Count != styles.Count)
            {
                _logger.LogWarning(
                    "Multi-model: Model ve Style sayıları eşleşmiyor. | ModelCount: {Mc} | StyleCount: {Sc}",
                    models.Count, styles.Count);

                return ServiceResult<GenerateMediaResponseDto>.Failure(
                    "Model ve Style sayıları eşleşmiyor.", 400);
            }

            // Üretilecek eleman sayısı: iki listenin büyüğü. İkisi de boşsa tek default eleman.
            var n = Math.Max(models.Count, styles.Count);
            if (n == 0) n = 1;

            _logger.LogInformation(
                "Multi-model generation isteği. | Count: {N} | Models: {Models} | Styles: {Styles}",
                n, dto.Model, dto.Style);

            // Character ile FaceLock mutually exclusive: CharacterId doluysa facelock alanı eklenmez
            // (CharacterDataDto zaten yüz tutarlılığı sağlar). Bu kural her eleman için
            // BuildGenerationElement içinde uygulanır; aşağıdaki bayraklar yalnızca loglama içindir.
            var hasCharacter = !string.IsNullOrWhiteSpace(dto.CharacterId);
            var hasFacelock = !string.IsNullOrWhiteSpace(dto.FaceLockAssetId) && !hasCharacter;

            // API kuralı: style ve model aynı objede bulunamaz (mutually exclusive).
            // Bu kural her eleman için ayrı uygulanır (bkz. BuildGenerationElement).
            var payloadElements = new List<object>();
            for (int i = 0; i < n; i++)
            {
                var modelForThis = i < models.Count ? models[i] : null;
                var styleForThis = i < styles.Count ? styles[i] : null;

                payloadElements.Add(BuildGenerationElement(dto, modelForThis, styleForThis));
            }

            var payload = payloadElements.ToArray();

            try
            {
                _logger.LogDebug(
                    "RenderNet API'ye istek gönderiliyor. | URL: {Url} | ElementCount: {Count} | UsesFacelock: {UsesFacelock} | UsesCharacter: {UsesCharacter}",
                    $"{_settings.BaseUrl}/generations", payload.Length, hasFacelock, hasCharacter);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_settings.BaseUrl}/generations", payload, _requestJsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    _logger.LogError(
                        "RenderNet API hatası! | StatusCode: {StatusCode} | Response: {Response}",
                        response.StatusCode, errorContent);

                    return ServiceResult<GenerateMediaResponseDto>.Failure(
                        "Görsel oluşturma servisi şu anda yanıt vermiyor. Lütfen tekrar deneyin.",
                        502);
                }

                // 🆕 JSON string yerine DTO'ya deserialize et
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GenerateMediaResponseDto>(content, _jsonOptions);

                if (result?.Data?.GenerationId == null)
                {
                    _logger.LogError(
                        "API yanıtından generation_id alınamadı! | RawContent: {Content}",
                        content);

                    return ServiceResult<GenerateMediaResponseDto>.Failure(
                        "Görsel oluşturma isteği işlenemedi. Lütfen tekrar deneyin.");
                }

                _logger.LogInformation(
                    "Görsel oluşturma isteği kabul edildi. | GenerationId: {GenId} | Status: {Status} | CreditsRemaining: {Credits}",
                    result.Data.GenerationId,
                    result.Data.Result,
                    result.Data.CreditsRemaining);

                return ServiceResult<GenerateMediaResponseDto>.Success(
                    result,
                    "Görsel oluşturma başlatıldı! Lütfen bekleyin...");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex,
                    "RenderNet API'ye bağlanılamadı. | Model: {Model}", dto.Model);

                return ServiceResult<GenerateMediaResponseDto>.Failure(
                    "Görsel oluşturma servisine bağlanılamadı. Lütfen tekrar deneyin.", 502);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex,
                    "RenderNet API zaman aşımı. | Model: {Model}", dto.Model);

                return ServiceResult<GenerateMediaResponseDto>.Failure(
                    "İstek zaman aşımına uğradı. Lütfen tekrar deneyin.", 504);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GenerateMedia beklenmeyen hata! | Model: {Model}", dto.Model);

                return ServiceResult<GenerateMediaResponseDto>.Failure(
                    "Görsel oluşturulurken beklenmeyen bir hata oluştu.");
            }
        }

        /// <summary>
        /// Tek bir generation elemanını (anonymous object) kurar.
        /// API kuralı gereği style ve model aynı objede bulunamaz:
        ///  - style doluysa: style koyulur, model koyulmaz.
        ///  - style boş ama model doluysa: model koyulur.
        ///  - ikisi de boşsa: hiçbiri koyulmaz, API default'a düşer.
        /// Character ve FaceLock mutually exclusive:
        ///  - CharacterId doluysa: character koyulur, facelock koyulmaz (asset_id form'dan
        ///    dolu gelse bile sessizce ignore edilir).
        ///  - CharacterId boş, FaceLockAssetId doluysa: facelock koyulur (Phase 1 davranışı).
        ///  - ikisi de boşsa: ne character ne facelock koyulur.
        /// Eklenmeyen alanlar null bırakılır → WhenWritingNull ile serialize edilmez.
        /// Character her style/model dalında AYNI objedir (tek karakter çoklu modelle).
        /// Pose Lock (control_net) bu kurallardan ORTHOGONAL'dir: character/facelock
        /// exclusivity'sinden bağımsız çalışır, PoseLockAssetId doluysa her dala
        /// AYNI control_net objesi eklenir (character ve/veya facelock ile birlikte var olabilir).
        /// </summary>
        private static object BuildGenerationElement(
            MediaGenerationRequestDto dto, string? model, string? style)
        {
            var promptObj = new
            {
                positive = dto.PositivePrompt,
                negative = dto.NegativePrompt
            };

            // Character koşullu: CharacterId doluysa kurulur, aksi halde null.
            var hasCharacter = !string.IsNullOrWhiteSpace(dto.CharacterId);
            object? characterObj = hasCharacter
                ? new
                {
                    character_id = dto.CharacterId,
                    mode = string.IsNullOrWhiteSpace(dto.CharacterMode) ? "balanced" : dto.CharacterMode
                }
                : null;

            // Facelock koşullu: yalnızca character YOKKEN ve asset_id doluyken eklenir (mutual exclusivity).
            object? facelockObj = (!hasCharacter && !string.IsNullOrWhiteSpace(dto.FaceLockAssetId))
                ? new { asset_id = dto.FaceLockAssetId }
                : null;

            // Pose Lock (control_net) koşullu ve orthogonal: PoseLockAssetId doluysa eklenir,
            // character/facelock durumundan BAĞIMSIZ. control_mode ve resize_mode integer enum.
            object? controlNetObj = !string.IsNullOrWhiteSpace(dto.PoseLockAssetId)
                ? new
                {
                    asset_id = dto.PoseLockAssetId,
                    control_mode = POSE_CONTROL_MODE,
                    name = POSE_NAME,
                    resize_mode = POSE_RESIZE_MODE
                }
                : null;

            if (!string.IsNullOrWhiteSpace(style))
            {
                return new
                {
                    aspect_ratio = dto.AspectRatio,
                    batch_size = dto.BatchSize,
                    cfg_scale = dto.CfgScale,
                    steps = dto.Steps,
                    seed = dto.Seed,
                    sampler = dto.Sampler,
                    quality = dto.Quality,
                    style = style,
                    character = characterObj,
                    facelock = facelockObj,
                    control_net = controlNetObj,
                    prompt = promptObj
                };
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                return new
                {
                    aspect_ratio = dto.AspectRatio,
                    batch_size = dto.BatchSize,
                    cfg_scale = dto.CfgScale,
                    steps = dto.Steps,
                    seed = dto.Seed,
                    sampler = dto.Sampler,
                    quality = dto.Quality,
                    model = model,
                    character = characterObj,
                    facelock = facelockObj,
                    control_net = controlNetObj,
                    prompt = promptObj
                };
            }

            return new
            {
                aspect_ratio = dto.AspectRatio,
                batch_size = dto.BatchSize,
                cfg_scale = dto.CfgScale,
                steps = dto.Steps,
                seed = dto.Seed,
                sampler = dto.Sampler,
                quality = dto.Quality,
                character = characterObj,
                facelock = facelockObj,
                control_net = controlNetObj,
                prompt = promptObj
            };
        }

        /// <summary>
        /// 🆕 Generation durumunu kontrol et (Polling)
        /// Frontend bu metodu belirli aralıklarla çağırır
        /// </summary>
        public async Task<ServiceResult<GetGenerationResponseDto>> GetGenerationAsync(string generationId)
        {
            _logger.LogDebug(
                "Generation durumu sorgulanıyor. | GenerationId: {GenId}",
                generationId);

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_settings.BaseUrl}/generations/{generationId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    _logger.LogError(
                        "Generation durumu alınamadı! | GenerationId: {GenId} | StatusCode: {StatusCode} | Response: {Response}",
                        generationId, response.StatusCode, errorContent);

                    return ServiceResult<GetGenerationResponseDto>.Failure(
                        "Görsel durumu kontrol edilemedi.", (int)response.StatusCode);
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GetGenerationResponseDto>(content, _jsonOptions);

                if (result?.Data == null)
                {
                    _logger.LogError(
                        "Generation yanıtı parse edilemedi! | GenerationId: {GenId} | RawContent: {Content}",
                        generationId, content);

                    return ServiceResult<GetGenerationResponseDto>.Failure(
                        "Görsel durumu okunamadı.");
                }

                // Media durumlarını logla
                foreach (var media in result.Data.Media ?? new List<GetGenerationMediaItemDto>())
                {
                    _logger.LogDebug(
                        "Media durumu: | MediaId: {MediaId} | Status: {Status} | HasUrl: {HasUrl}",
                        media.Id, media.Status, !string.IsNullOrEmpty(media.Url));
                }

                return ServiceResult<GetGenerationResponseDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GetGeneration beklenmeyen hata! | GenerationId: {GenId}",
                    generationId);

                return ServiceResult<GetGenerationResponseDto>.Failure(
                    "Görsel durumu kontrol edilirken bir hata oluştu.");
            }
        }
    }
}