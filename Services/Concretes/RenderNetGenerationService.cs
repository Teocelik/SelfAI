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
            _logger.LogInformation(
                "Görsel oluşturma isteği başlatıldı. | Model: {Model} | AspectRatio: {AspectRatio} | BatchSize: {BatchSize}",
                dto.Model, dto.AspectRatio, dto.BatchSize);

            // API kuralı: style ve model aynı objede bulunamaz (mutually exclusive).
            // Style seçildiyse style gönderilir, yoksa model gönderilir.
            // Facelock opsiyoneldir: asset_id boşsa alan hiç gönderilmez (WhenWritingNull).
            var hasStyle = !string.IsNullOrWhiteSpace(dto.Style);
            var hasFacelock = !string.IsNullOrWhiteSpace(dto.FaceLockAssetId);

            var promptObj = new
            {
                positive = dto.PositivePrompt,
                negative = dto.NegativePrompt
            };

            var facelockObj = hasFacelock
                ? (object?)new { asset_id = dto.FaceLockAssetId }
                : null;

            object item;
            if (hasStyle)
            {
                item = new
                {
                    aspect_ratio = dto.AspectRatio,
                    batch_size = dto.BatchSize,
                    cfg_scale = dto.CfgScale,
                    steps = dto.Steps,
                    seed = dto.Seed,
                    sampler = dto.Sampler,
                    quality = dto.Quality,
                    style = dto.Style,
                    facelock = facelockObj,
                    prompt = promptObj
                };
            }
            else
            {
                item = new
                {
                    aspect_ratio = dto.AspectRatio,
                    batch_size = dto.BatchSize,
                    cfg_scale = dto.CfgScale,
                    steps = dto.Steps,
                    seed = dto.Seed,
                    sampler = dto.Sampler,
                    quality = dto.Quality,
                    model = dto.Model,
                    facelock = facelockObj,
                    prompt = promptObj
                };
            }

            var payload = new[] { item };

            try
            {
                _logger.LogDebug(
                    "RenderNet API'ye istek gönderiliyor. | URL: {Url} | UsesStyle: {UsesStyle} | UsesFacelock: {UsesFacelock}",
                    $"{_settings.BaseUrl}/generations", hasStyle, hasFacelock);

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