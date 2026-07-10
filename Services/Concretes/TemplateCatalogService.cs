using SelfAI.DTOs.Templates;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes;

/// <summary>
/// F.M.10a statik format kataloğu. 6 sabit sosyal medya formatı. Format→Model
/// eşlemesi kilitli (F.9'a kadar): Ideogram V3 (image_size-native) veya
/// Nano Banana (aspect_ratio-native). Boyut metadata'sı frontend'de sonuç kartı
/// için kullanılır (fal.ai response width/height'ına güvenilmez).
/// </summary>
public class TemplateCatalogService : ITemplateCatalogService
{
    private static readonly IReadOnlyList<PostFormatDto> Formats = new List<PostFormatDto>
    {
        new PostFormatDto
        {
            Id = "instagram-post",
            DisplayName = "Instagram Post",
            Description = "1080×1080, kare format, Instagram feed için",
            Platform = "instagram",
            Width = 1080,
            Height = 1080,
            ModelEndpoint = "fal-ai/ideogram/v3",
            AspectRatioValue = "1:1",
            PromptSuffix = ", centered composition, clean background, product photography aesthetic, high quality"
        },
        new PostFormatDto
        {
            Id = "instagram-story",
            DisplayName = "Instagram Story",
            Description = "1080×1920, dikey format, 24 saatlik hikayeler için",
            Platform = "instagram",
            Width = 1080,
            Height = 1920,
            ModelEndpoint = "fal-ai/nano-banana",
            AspectRatioValue = "9:16",
            PromptSuffix = ", vertical composition, mobile-friendly framing, vibrant colors"
        },
        new PostFormatDto
        {
            Id = "tiktok-reels-cover",
            DisplayName = "TikTok / Reels Kapak",
            Description = "1080×1920, video kapak görseli, dikkat çekici kompozisyon",
            Platform = "tiktok",
            Width = 1080,
            Height = 1920,
            ModelEndpoint = "fal-ai/nano-banana",
            AspectRatioValue = "9:16",
            PromptSuffix = ", eye-catching composition, dynamic pose, cinematic lighting, thumbnail style"
        },
        new PostFormatDto
        {
            Id = "youtube-thumbnail",
            DisplayName = "YouTube Thumbnail",
            Description = "1280×720, video kapak görseli, tıklama çekici",
            Platform = "youtube",
            Width = 1280,
            Height = 720,
            ModelEndpoint = "fal-ai/ideogram/v3",
            AspectRatioValue = "16:9",
            PromptSuffix = ", bold composition, high contrast, thumbnail style, attention-grabbing"
        },
        new PostFormatDto
        {
            Id = "youtube-shorts",
            DisplayName = "YouTube Shorts Kapak",
            Description = "1080×1920, kısa video kapak görseli",
            Platform = "youtube",
            Width = 1080,
            Height = 1920,
            ModelEndpoint = "fal-ai/nano-banana",
            AspectRatioValue = "9:16",
            PromptSuffix = ", vertical composition, engaging visual, mobile-optimized"
        },
        new PostFormatDto
        {
            Id = "facebook-linkedin-post",
            DisplayName = "Facebook / LinkedIn Post",
            Description = "16:9 yaklaşımı, kurumsal ve profesyonel görseller için",
            Platform = "linkedin",
            Width = 1200,
            Height = 630,
            ModelEndpoint = "fal-ai/ideogram/v3",
            AspectRatioValue = "16:9",
            PromptSuffix = ", professional composition, corporate aesthetic, editorial photography style"
        }
    };

    public IReadOnlyList<PostFormatDto> GetAllFormats() => Formats;

    public PostFormatDto? GetFormat(string formatId) =>
        Formats.FirstOrDefault(f => f.Id.Equals(formatId, StringComparison.OrdinalIgnoreCase));
}
