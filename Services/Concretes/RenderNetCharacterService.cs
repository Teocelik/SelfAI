using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.RenderNetCharacterResponseDtos;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class RenderNetCharacterService : IRenderNetCharacterService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetOptions _settings;
        public RenderNetCharacterService(HttpClient httpClient, IOptions<RenderNetOptions> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        //public Task<CharacterResponseDto> CreateCharacter()
        //{
        //    // Payload oluşturalım
            
        //}
    }
}
