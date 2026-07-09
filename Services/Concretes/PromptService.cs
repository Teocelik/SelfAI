using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class PromptService : IPromptService
    {
        private readonly ILogger<PromptService> _logger;

        // F.M.UI.1c: Rastgele öneri havuzu profesyonel örneklerle değiştirildi.
        // İngilizce ağırlıklı — fal.ai İngilizce prompt'larda daha kararlı sonuç veriyor.
        // Kullanıcı yine kendi (Türkçe dahil) prompt'unu yazabilir; bu yalnızca "dice" önerisi.
        private readonly IReadOnlyList<string> _prompts = new List<string>
        {
            // Portre & Character
            "cinematic portrait of a young woman with soft window light, film grain, shallow depth of field, 35mm lens",
            "close-up portrait, dramatic side lighting, deep shadows, moody atmosphere, high contrast, professional photography",
            "editorial fashion shot, model wearing minimal black outfit, studio lighting, clean white background, high fashion aesthetic",
            "environmental portrait in urban setting, golden hour, natural lighting, storytelling composition, medium format film look",

            // Sahne & Character in scene
            "a person walking through misty forest at dawn, atmospheric, backlit, cinematic color grading, dreamy mood",
            "figure sitting in a rooftop cafe overlooking city skyline at sunset, warm tones, lifestyle photography",
            "solo traveler on empty beach, wide shot, golden hour, painterly composition, muted color palette",
            "person reading in a sunlit library, warm interior light, dust particles in air, contemplative mood",

            // Fashion & Product
            "flat lay of luxury skincare products on marble surface, soft daylight, minimalist composition, editorial style",
            "fashion accessory on textured background, macro detail, moody lighting, brand campaign aesthetic",
            "streetwear outfit flat lay with sneakers, watch, and sunglasses, urban aesthetic, top-down view",

            // Landscape & Nature
            "misty mountain landscape at sunrise, layered depth, atmospheric perspective, moody color grading",
            "dramatic ocean waves at storm, long exposure, black and white, fine art photography",
            "autumn forest path with fallen leaves, dappled sunlight, painterly quality, cinematic wide shot",

            // Şehir & Urban
            "neon-lit alley in Tokyo at night, rain reflections, cyberpunk aesthetic, cinematic composition",
            "Istanbul skyline from the Bosphorus at dusk, warm golden light, atmospheric haze, editorial travel photography",
            "empty city street at blue hour, film photography aesthetic, long shadows, minimal composition",

            // Sanat Stili
            "oil painting portrait in Rembrandt style, chiaroscuro lighting, rich earth tones, museum quality",
            "watercolor illustration of a coastal village, soft edges, pastel palette, artistic textured paper",
            "modern minimalist illustration, flat design, bold geometric shapes, limited color palette",

            // İç mekan & Yaşam
            "cozy scandinavian interior with morning light, wooden textures, minimalist aesthetic, lifestyle magazine style",
            "vintage cafe interior, warm ambient light, film photography, nostalgic mood, muted tones",

            // Yiyecek & Product
            "artisan coffee cup on wooden table with steam rising, moody lighting, close-up macro, coffee shop aesthetic",
            "gourmet dish plated minimalistically, overhead view, natural light, restaurant menu photography",

            // Karakter aksiyonu
            "athletic person mid-run on empty street at sunrise, dynamic pose, motion blur, sports photography aesthetic",
            "creative professional at desk with laptop and coffee, natural window light, candid moment, lifestyle brand imagery"
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
