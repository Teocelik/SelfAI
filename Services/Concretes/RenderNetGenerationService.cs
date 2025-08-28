using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.RenderNetGenerationRequestDtos;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class RenderNetGenerationService : IRenderNetGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetOptions _settings;

        public RenderNetGenerationService(HttpClient httpClient, IOptions<RenderNetOptions> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;

            //Headersları ayarlayalım
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
        }

        // asset_id + prompt = AI görsel
        public Task<string> GenerateMediaAsync(string assetId, MediaGenerationRequestDto dto)
        {
            if(string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentException("Asset ID cannot be null or empty.", nameof(assetId));
            }

            //Payload oluştur(UploadUrl'den asset_id'yi al)
            var testPayload = new[]
            {
                    new
                    {
                        aspect_ratio = dto.AspectRatio,
                        batch_size = dto.BatchSize, //Oluşturulacak görsel sayısı
                        cfg_scale = 7,
                        model = dto.Model,
                        style = "Realistic",
                        steps = 25,
                        seed = 42,

                        facelock = new
                        {
                            asset_id = assetId, // UploadUrl'den alınan asset_id
                        },

                        prompt = new
                        {
                            //Kullanıcının promptu veya hazır promptlardan biri
                            positive =dto.PositivePrompt,
                            negative = "nsfw, deformed, extra limbs, bad anatomy, deformed pupils, text, worst quality, jpeg artifacts, ugly, duplicate, morbid, mutilated"
                        },
                        quality = "Standard",
                        sampler = "DPM++ 2M Karras",
                    }
            };

            //Httpclient'a request gönder ve response al
            var payloadResponse = _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}/generations", testPayload);
            //Response içeriğini oku ve serilize et
            var payloadContent = payloadResponse.Result.Content.ReadAsStringAsync();

            return Task.FromResult(payloadContent.Result);
        }
    }
}
