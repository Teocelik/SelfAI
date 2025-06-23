using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SelfAI.DTOs.RenderNet;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using System.Drawing;
using System.Text.Json;

namespace SelfAI.Services.Concretes
{
    public class RenderNetAssetService : IRenderNetAssetService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetSettings _settings;

        public RenderNetAssetService(HttpClient httpClient, IOptions<RenderNetSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;

            //Headerlari ayarlayalım
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        // Bu metot, RenderNet API'sine bir varlık yükler.
        public async Task<string> UploadAssetAsync(object payload)
        {
            // Path yazalım
            var response = await _httpClient.PostAsJsonAsync($"{_httpClient.BaseAddress}/assets/upload", payload);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // Bu metot, RenderNet API'sinden bir varlığı alır.
        public Task<string> GetAssetAsync(UploadAssetResponse asset)
        {
            throw new NotImplementedException();
        }

        // Bu metot, RenderNet API'sinden bir yükleme URL'si alır.
        public async Task<UploadAssetResponse> GetUploadUrlAsync()
        {
            // Yükleme URL'si almak için gerekli payload'u oluşturalım
            
            var payload = new
            {
                size = new { height = 512, width = 512 }
            };

            // payloadı Json olarak serilize edip httpcontent olarak PostAsync metoduna verelim
            var payloadContent = JsonContent.Create(payload);

            // RenderNet API'sine yükleme URL'si almak için istek yapalım
            var response = await _httpClient.PostAsync($"{_httpClient.BaseAddress}/assets/upload", payloadContent);

            // Eğer response başarılı değilse, bir hata fırlatalım
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Yükleme URL'si alınamadı. Hata kodu: {response.StatusCode}");
            }

            // response içeriğini okuyalım
            var content = await response.Content.ReadAsStringAsync();

            // içeriği deserilize edip UploadAssetData nesnesine dönüştürelim
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var serilizedResponse = JsonSerializer.Deserialize<UploadAssetResponse>(content, options);

            // Eğer serilizezResponse null ise, bir hata fırlatalım
            if (serilizedResponse == null)
            {
                throw new Exception("Yükleme URL'si alınamadı.");
            }

            return serilizedResponse;
        }
    }
}
