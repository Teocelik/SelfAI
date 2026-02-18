using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.RenderNetResourceDtos;
using SelfAI.Services.Interfaces;
using System.Text.Json;

namespace SelfAI.Services.Concretes
{
    public class RenderNetResourcesService : IRenderNetResourcesService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetOptions _settings;

        public RenderNetResourcesService(HttpClient httpClient, IOptions<RenderNetOptions> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;

            //Headerlari ayarlayalım
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        // flux image style'lerini API'den çeker
        public async Task<FluxImageStyleRootDto> GetFluxStylesAsync()
        {
            // request oluşturalım
            var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}/styles?type=flux&page=1&page_size=17");

            // Eğer response başarılı değilse, bir hata fırlatalım
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Flux styles alınamadı. Hata kodu: {response.StatusCode}");
            }

            // response içeriğini okuyalım
            var content = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<FluxImageStyleRootDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null)
            {
                throw new Exception($"Flux stiller alınamadı! Hata kodu {response.StatusCode}");
            }

            return result;
        }
    }
}
