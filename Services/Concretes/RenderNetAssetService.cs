using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.RenderNet;
using SelfAI.DTOs.RenderNetUploadResponseDtos;
using SelfAI.Models; // 🆕
using SelfAI.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SelfAI.Services.Concretes
{
    public class RenderNetAssetService : IRenderNetAssetService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetOptions _settings;
        private readonly ILogger<RenderNetAssetService> _logger;

        public RenderNetAssetService(HttpClient httpClient, IOptions<RenderNetOptions> options, ILogger<RenderNetAssetService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        public async Task<ServiceResult<UploadAssetResponseDto>> GetAssetIdAsync(UploadAssetRequestDto request)
        {
            // ═══════════════════════════════════════
            // AŞAMA 1: Upload URL al
            // ═══════════════════════════════════════
            _logger.LogInformation(
                "Asset yükleme başlatıldı. | FileName: {FileName} | Size: {Size} KB | ContentType: {ContentType}",
                request.FileName, request.Length / 1024, request.ContentType);

            try
            {
                var payload = new
                {
                    size = new { height = 512, width = 512 }
                };
                var payloadContent = JsonContent.Create(payload);

                _logger.LogDebug("Upload URL isteniyor. | URL: {Url}", $"{_httpClient.BaseAddress}/assets/upload");

                var response = await _httpClient.PostAsync($"{_httpClient.BaseAddress}/assets/upload", payloadContent);

                // Upload URL alınamadı
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Detayı LOGLA (sadece biz görürüz)
                    _logger.LogError("Upload URL alınamadı! | StatusCode: {StatusCode} | Response: {Response}", response.StatusCode, errorContent);

                    // Kullanıcıya GÜVENLİ mesaj dön
                    return ServiceResult<UploadAssetResponseDto>.Failure("Dosya yükleme servisi şu anda kullanılamıyor. Lütfen tekrar deneyin.", 502);
                }

                var content = await response.Content.ReadAsStringAsync();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var serializedResponse = JsonSerializer.Deserialize<UploadAssetResponseDto>(content, jsonOptions);

                // Deserialize başarısız
                if (serializedResponse == null)
                {
                    _logger.LogError("Upload URL yanıtı deserialize edilemedi! | RawContent: {Content}",content);

                    return ServiceResult<UploadAssetResponseDto>.Failure("Sunucudan gelen yanıt işlenemedi. Lütfen tekrar deneyin.");
                }

                _logger.LogInformation("Upload URL başarıyla alındı. | AssetId: {AssetId}", serializedResponse.Data?.Asset?.Id);

                // ═══════════════════════════════════════
                // AŞAMA 2: Dosyayı Upload URL'e yükle
                // ═══════════════════════════════════════
                _logger.LogDebug("Dosya Upload URL'ye yükleniyor. | UploadUrl: {Url}", serializedResponse.Data?.UploadUrl);

                // Stream'i başa sar (eğer önceden okunduysa)
                if (request.Content.CanSeek)
                {
                    request.Content.Position = 0;
                }

                using var streamContent = new StreamContent(request.Content);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);

                if(serializedResponse.Data?.UploadUrl == null)
                {
                    _logger.LogError("Upload URL yanıtında UploadUrl alanı null! | RawContent: {Content}", content);
                    return ServiceResult<UploadAssetResponseDto>.Failure("Sunucudan gelen yanıt eksik. Lütfen tekrar deneyin.");
                }

                var uploadResponse = await _httpClient.PutAsync(serializedResponse.Data.UploadUrl, streamContent);

                // Dosya yükleme başarısız
                if (!uploadResponse.IsSuccessStatusCode)
                {
                    var uploadError = await uploadResponse.Content.ReadAsStringAsync();

                    _logger.LogError("Dosya yükleme başarısız! | StatusCode: {StatusCode} | AssetId: {AssetId} | Response: {Response}", uploadResponse.StatusCode, serializedResponse.Data?.Asset?.Id,uploadError);

                    return ServiceResult<UploadAssetResponseDto>.Failure("Fotoğraf yüklenirken bir hata oluştu. Lütfen tekrar deneyin.", 502);
                }

                // Her iki aşama da başarılı!
                _logger.LogInformation( "Asset yükleme tamamlandı. | AssetId: {AssetId} | FileName: {FileName}",serializedResponse.Data?.Asset?.Id,
                    request.FileName);

                return ServiceResult<UploadAssetResponseDto>.Success(serializedResponse, "Fotoğraf başarıyla yüklendi!");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex,"Asset servisine bağlanılamadı. | FileName: {FileName}", request.FileName);

                return ServiceResult<UploadAssetResponseDto>.Failure("Dosya yükleme servisine bağlanılamadı. İnternet bağlantınızı kontrol edin.", 502);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex,"Asset yükleme zaman aşımı. | FileName: {FileName}", request.FileName);

                return ServiceResult<UploadAssetResponseDto>.Failure("Dosya yükleme zaman aşımına uğradı. Lütfen tekrar deneyin.",504);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Asset yükleme beklenmeyen hata! | FileName: {FileName}", request.FileName);

                return ServiceResult<UploadAssetResponseDto>.Failure("Dosya yüklenirken beklenmeyen bir hata oluştu.");
            }
        }
    }
}