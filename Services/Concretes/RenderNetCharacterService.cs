using Microsoft.Extensions.Options;
using SelfAI.DTOs.RenderNetCharacterResponseDtos;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class RenderNetCharacterService : IRenderNetCharacterService
    {
        private readonly HttpClient _httpClient;
        private readonly RenderNetSettings _settings;
        public RenderNetCharacterService(HttpClient httpClient, IOptions<RenderNetSettings> options)
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
