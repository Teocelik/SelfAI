using Microsoft.Extensions.Options;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class RenderNetService : IRenderNetService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetSettings _settings;
        public RenderNetService(HttpClient httpClient, IOptions<RenderNetSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;

            //Headerlari ayarlayalım
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        // Bu metot, RenderNet API'sine bir varlık yükler ve yanıt olarak dönen veriyi string olarak döner.
        public async Task<string> UploadAssetAsync(object payload)
        {
            // Path yazalım
            var response = await _httpClient.PostAsJsonAsync("/assets/upload", payload);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

    }
}
