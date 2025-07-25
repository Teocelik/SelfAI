﻿using Microsoft.Extensions.Options;
using SelfAI.Configurations;
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
        public Task<string> GenerateMediaAsync(string assetId)
        {
            if(string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentException("Asset ID cannot be null or empty.", nameof(assetId));
            }
            try
            {
                //Payload oluştur(UploadUrl'den asset_id'yi al)
                var testPayload = new[]
                {
                    new
                    {
                        aspect_ratio = "1:1",
                        batch_size = 1,
                        cfg_scale = 7,
                        model = "JuggernautXL",
                        style = "Realistic",
                        steps = 25,
                        seed = 42,

                        facelock = new
                        {
                            asset_id = assetId, // UploadUrl'den alınan asset_id
                        },

                        prompt = new
                        {
                            positive = "A beautiful landscape with mountains and a river",
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
            catch(Exception ex)
            {
                throw new Exception("An error occurred while generating media.", ex);
            }
        }
    }
}
