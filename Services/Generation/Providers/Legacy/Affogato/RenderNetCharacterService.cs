using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.Legacy.RenderNet.Character;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace SelfAI.Services.Generation.Providers.Legacy.Affogato
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

        // Yeni karakter oluşturur (POST /pub/v1/characters). Dönen data id + input_image içerir.
        public async Task<ServiceResult<CharacterDataDto>> CreateCharacterAsync(
            string assetId, string name, string prompt, string characterType)
        {
            var request = new CharacterCreateRequestDto
            {
                AssetId = assetId,
                CharacterType = characterType,
                Name = name,
                Prompt = prompt
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_httpClient.BaseAddress}/characters", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Affogato karakter oluşturma hatası. | StatusCode: {StatusCode} | Body: {Body}",
                    (int)response.StatusCode, errorBody);

                return ServiceResult<CharacterDataDto>.Failure(
                    "Karakter oluşturulamadı. Lütfen daha sonra tekrar deneyin.", 502);
            }

            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<CharacterCreateResponseDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Data is null || string.IsNullOrWhiteSpace(result.Data.Id))
            {
                _logger.LogWarning(
                    "Affogato karakter oluşturma yanıtı ayrıştırılamadı. | Name: {Name}",
                    name);

                return ServiceResult<CharacterDataDto>.Failure(
                    "Karakter oluşturulamadı. Lütfen daha sonra tekrar deneyin.", 502);
            }

            _logger.LogInformation(
                "Affogato karakter oluşturuldu. | AffId: {AffId} | Name: {Name}",
                result.Data.Id, result.Data.Name);

            return ServiceResult<CharacterDataDto>.Success(result.Data, "Karakter oluşturuldu");
        }

        // Karakteri arşivler (DELETE /pub/v1/characters/{id}). F.6.4'te endpoint doğrulanacak (paid API gerekli).
        public async Task<ServiceResult<bool>> ArchiveCharacterAsync(string characterId)
        {
            var response = await _httpClient.DeleteAsync(
                $"{_httpClient.BaseAddress}/characters/{characterId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Affogato karakter arşivleme hatası. | AffId: {AffId} | StatusCode: {StatusCode}",
                    characterId, (int)response.StatusCode);

                return ServiceResult<bool>.Failure(
                    "Karakter arşivlenemedi. Lütfen daha sonra tekrar deneyin.", 502);
            }

            _logger.LogInformation("Affogato karakter arşivlendi. | AffId: {AffId}", characterId);

            return ServiceResult<bool>.Success(true, "Karakter arşivlendi");
        }
    }
}
