using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.Legacy.RenderNet.Resource;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using System.Text.Json;

namespace SelfAI.Services.Generation.Providers.Legacy.Affogato
{
    public class RenderNetResourcesService : IRenderNetResourcesService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetOptions _settings;
        private readonly ILogger<RenderNetResourcesService> _logger;

        public RenderNetResourcesService(HttpClient httpClient, IOptions<RenderNetOptions> options, ILogger<RenderNetResourcesService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;

            //Headerlari ayarlayalım
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        // Stilleri API'den çeker (GetModelsAsync ile aynı kalıp; ServiceResult + structured log)
        public async Task<ServiceResult<IReadOnlyList<FluxImageSytleDetailDto>>> GetStylesAsync(
            string type = "flux",
            int pageSize = 50)
        {
            // request oluşturalım (BaseAddress sonda slash içermediği için tam URL kuruyoruz)
            var response = await _httpClient.GetAsync(
                $"{_httpClient.BaseAddress}/styles?type={type}&page=1&page_size={pageSize}");

            // Eğer response başarılı değilse, kullanıcıya güvenli bir mesaj dönelim
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Stiller alınamadı. | Type: {Type} | StatusCode: {StatusCode}",
                    type, (int)response.StatusCode);

                return ServiceResult<IReadOnlyList<FluxImageSytleDetailDto>>.Failure(
                    "Stiller getirilemedi. Lütfen daha sonra tekrar deneyin.", 502);
            }

            // response içeriğini okuyalım
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<FluxImageStyleRootDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Data is null)
            {
                _logger.LogWarning(
                    "Stil yanıtı ayrıştırılamadı. | Type: {Type} | StatusCode: {StatusCode}",
                    type, (int)response.StatusCode);

                return ServiceResult<IReadOnlyList<FluxImageSytleDetailDto>>.Failure(
                    "Stiller getirilemedi. Lütfen daha sonra tekrar deneyin.", 502);
            }

            _logger.LogInformation(
                "Stiller getirildi. | Type: {Type} | Count: {Count}",
                type, result.Data.Count);

            return ServiceResult<IReadOnlyList<FluxImageSytleDetailDto>>.Success(
                result.Data, "Stiller getirildi");
        }

        // Flux modellerini API'den çeker (GetStylesAsync ile aynı kalıp; ServiceResult + structured log)
        public async Task<ServiceResult<IReadOnlyList<ModelInfoDto>>> GetModelsAsync(
            string type = "flux",
            int pageSize = 50)
        {
            // request oluşturalım (BaseAddress sonda slash içermediği için tam URL kuruyoruz, GetStylesAsync ile aynı)
            var response = await _httpClient.GetAsync(
                $"{_httpClient.BaseAddress}/models?type={type}&page=1&page_size={pageSize}");

            // Eğer response başarılı değilse, kullanıcıya güvenli bir mesaj dönelim
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Modeller alınamadı. | Type: {Type} | StatusCode: {StatusCode}",
                    type, (int)response.StatusCode);

                return ServiceResult<IReadOnlyList<ModelInfoDto>>.Failure(
                    "Modeller getirilemedi. Lütfen daha sonra tekrar deneyin.", 502);
            }

            // response içeriğini okuyalım
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ModelRootDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Data is null)
            {
                _logger.LogWarning(
                    "Model yanıtı ayrıştırılamadı. | Type: {Type} | StatusCode: {StatusCode}",
                    type, (int)response.StatusCode);

                return ServiceResult<IReadOnlyList<ModelInfoDto>>.Failure(
                    "Modeller getirilemedi. Lütfen daha sonra tekrar deneyin.", 502);
            }

            _logger.LogInformation(
                "Modeller getirildi. | Type: {Type} | Count: {Count}",
                type, result.Data.Count);

            return ServiceResult<IReadOnlyList<ModelInfoDto>>.Success(
                result.Data, "Modeller getirildi");
        }
    }
}
