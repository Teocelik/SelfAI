using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SelfAI.DTOs.RenderNet;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using System.Drawing;
using System.Net.Http.Headers;
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

        // Bu metot, RenderNet API'sine varlık yüklemek için kullanılır.
        public async Task<UploadAssetResponse> UploadAssetAsync(IFormFile assetFile)
        {
            if (assetFile == null || assetFile.Length == 0)
            {
                throw new ArgumentException("Dosya boş olamaz.");
            }

            // Upload URL almak için GetUploadUrlAsync metodunu çağırıyoruz
            var uploadResponse = await GetUploadUrlAsync();

            if (uploadResponse == null)
            {
                throw new InvalidOperationException("Upload URL alınamadı.");
            }
            // OpenReadStream ile dosyanın içeriğini okuyoruz
            using var stream = assetFile.OpenReadStream();
            // StreamContent oluşturuyoruz ve içeriği ayarlıyoruz
            using var content = new StreamContent(stream);
            // Dosyanın tipini bildiriyoruz (image/png, image/jpeg vs.)
            content.Headers.ContentType = new MediaTypeHeaderValue(assetFile.ContentType);
            // Put isteği ile dosyayı yüklüyoruz
            var response = await _httpClient.PutAsync(uploadResponse.Data.UploadUrl, content);
            // Eğer istek başarılı değilse, bir hata fırlatıyoruz
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Dosya yükleme başarısız. Hata kodu: {response.StatusCode}");
            }
            // Yükleme başarılı ise, UploadAssetResponse nesnesini döndürüyoruz
            return uploadResponse;
        }
    }
}
