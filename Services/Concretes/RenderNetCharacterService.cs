using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.RenderNetCharacterResponseDtos;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using System.Text.Json;

namespace SelfAI.Services.Concretes
{
    public class RenderNetCharacterService : IRenderNetCharacterService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetOptions _settings;
        private readonly ILogger<RenderNetCharacterService> _logger;

        public RenderNetCharacterService(HttpClient httpClient, IOptions<RenderNetOptions> options, ILogger<RenderNetCharacterService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;

            //Headerlari ayarlayalım
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        // Karakterleri API'den çeker (GetModelsAsync ile aynı kalıp; ServiceResult + structured log)
        public async Task<ServiceResult<IReadOnlyList<CharacterDataDto>>> GetCharactersAsync(
            int page = 1,
            int pageSize = 50)
        {
            // request oluşturalım (BaseAddress sonda slash içermediği için tam URL kuruyoruz, GetModelsAsync ile aynı)
            var response = await _httpClient.GetAsync(
                $"{_httpClient.BaseAddress}/characters?page={page}&page_size={pageSize}");

            // Eğer response başarılı değilse, kullanıcıya güvenli bir mesaj dönelim
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Karakterler alınamadı. | Page: {Page} | StatusCode: {StatusCode}",
                    page, (int)response.StatusCode);

                return ServiceResult<IReadOnlyList<CharacterDataDto>>.Failure(
                    "Karakterler getirilemedi. Lütfen daha sonra tekrar deneyin.", 502);
            }

            // response içeriğini okuyalım
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<CharacterListResponseDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Data is null)
            {
                _logger.LogWarning(
                    "Karakter yanıtı ayrıştırılamadı. | Page: {Page} | StatusCode: {StatusCode}",
                    page, (int)response.StatusCode);

                return ServiceResult<IReadOnlyList<CharacterDataDto>>.Failure(
                    "Karakterler getirilemedi. Lütfen daha sonra tekrar deneyin.", 502);
            }

            _logger.LogInformation(
                "Karakterler getirildi. | Page: {Page} | Count: {Count}",
                page, result.Data.Count);

            return ServiceResult<IReadOnlyList<CharacterDataDto>>.Success(
                result.Data ?? new List<CharacterDataDto>(), "Karakterler getirildi");
        }
    }
}
