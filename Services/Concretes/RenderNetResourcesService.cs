using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Services.Interfaces;

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
        public Task<string> ListFluxStyles()
        {
            throw new NotImplementedException();
        }
    }
}
