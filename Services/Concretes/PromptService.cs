using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class PromptService : IPromptService
    {
        private readonly ILogger<PromptService> _logger;

        private readonly IReadOnlyList<string> _prompts = new List<string>
        {
            "A majestic dragon soaring through a sunset sky with golden clouds, fantasy art style, highly detailed, 4K resolution",
            "Cyberpunk cityscape at night with neon lights reflecting on wet streets, futuristic architecture, atmospheric lighting",
            "Enchanted forest with glowing mushrooms, fairy lights, magical atmosphere, digital art style, vibrant colors",
            "Ancient castle on a cliff overlooking a stormy ocean, dramatic lighting, gothic architecture, dark fantasy",
            "Space explorer walking on an alien planet with twin moons, sci-fi concept art, cinematic composition",
            "Steampunk airship flying above Victorian city, brass and copper details, industrial aesthetic, vintage technology",
            "Underwater palace made of coral and pearls, mermaids swimming nearby, bioluminescent sea life, ethereal lighting",
            "Post-apocalyptic wasteland with abandoned buildings, overgrown vegetation, dramatic sky, concept art style",
            "Japanese temple in cherry blossom season, traditional architecture, peaceful atmosphere, spring colors",
            "Robot warrior in futuristic armor, mechanical details, glowing energy weapons, dynamic action pose"
        };

        public PromptService(ILogger<PromptService> logger)
        {
            _logger = logger;
        }

        public ServiceResult<IReadOnlyList<string>> GetAll()
        {
            _logger.LogInformation(
                "Promptlar getirildi. | Count: {Count}",
                _prompts.Count);

            return ServiceResult<IReadOnlyList<string>>.Success(_prompts, "Prompt listesi getirildi.");
        }
    }
}
