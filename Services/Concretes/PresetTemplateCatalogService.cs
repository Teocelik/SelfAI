using SelfAI.DTOs.Templates;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes;

/// <summary>
/// Hazır şablon kataloğu (F.M.10b) — 5 sabit preset kod içinde tanımlı. Text alanlarının
/// koordinat/font/renk spec'i sabittir; kullanıcı yalnızca metni doldurur. F.9b'de DB CRUD.
/// </summary>
public class PresetTemplateCatalogService : IPresetTemplateCatalogService
{
    private static readonly IReadOnlyList<PresetTemplateDto> Presets = new List<PresetTemplateDto>
    {
        // Preset 1: Instagram Story — Yeni Ürün Duyurusu
        new PresetTemplateDto
        {
            Id = "ig-story-new-product",
            DisplayName = "Instagram Story — Yeni Ürün Duyurusu",
            Description = "Ürün fotoğrafı + başlık + fiyat + CTA. Story feed için 1080×1920.",
            Platform = "instagram",
            Width = 1080,
            Height = 1920,
            ModelEndpoint = "fal-ai/nano-banana",
            AspectRatioValue = "9:16",
            ImagePromptSuffix = ", product photography, clean minimal background, professional lighting, editorial style",
            PreviewImageUrl = "/img/presets/ig-story-new-product.jpg",
            TextFields = new List<PresetTextFieldDto>
            {
                new PresetTextFieldDto
                {
                    Id = "title", Label = "Başlık", Placeholder = "Örn: Yeni Sezon Ürünleri", MaxLength = 40,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 80, Y = 1400, MaxWidth = 920, FontSize = 72,
                        FontName = "Inter-Bold", Color = "#FFFFFF", Alignment = "left", DropShadow = true
                    }
                },
                new PresetTextFieldDto
                {
                    Id = "subtitle", Label = "Alt yazı", Placeholder = "Örn: Şimdi %30 indirimli", MaxLength = 60,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 80, Y = 1520, MaxWidth = 920, FontSize = 42,
                        FontName = "Inter-Regular", Color = "#FFFFFF", Alignment = "left", DropShadow = true
                    }
                },
                new PresetTextFieldDto
                {
                    Id = "cta", Label = "CTA", Placeholder = "Örn: Şimdi Alışverişe Başla", MaxLength = 30,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 80, Y = 1750, MaxWidth = 920, FontSize = 36,
                        FontName = "Inter-SemiBold", Color = "#00CED1", Alignment = "left", DropShadow = true
                    }
                }
            }
        },

        // Preset 2: YouTube Thumbnail — Vlog Stili
        new PresetTemplateDto
        {
            Id = "yt-thumbnail-vlog",
            DisplayName = "YouTube Thumbnail — Vlog Stili",
            Description = "Büyük başlık + emoji-vurgulu alt yazı. 1280×720 tıklama çekici.",
            Platform = "youtube",
            Width = 1280,
            Height = 720,
            ModelEndpoint = "fal-ai/ideogram/v3",
            AspectRatioValue = "16:9",
            ImagePromptSuffix = ", eye-catching composition, high contrast, thumbnail style, bold visual",
            PreviewImageUrl = "/img/presets/yt-thumbnail-vlog.jpg",
            TextFields = new List<PresetTextFieldDto>
            {
                new PresetTextFieldDto
                {
                    Id = "title", Label = "Ana Başlık", Placeholder = "Örn: BÜYÜK SÜRPRIZ!", MaxLength = 50,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 60, Y = 500, MaxWidth = 900, FontSize = 84,
                        FontName = "Inter-Bold", Color = "#FFFFFF", Alignment = "left", DropShadow = true
                    }
                },
                new PresetTextFieldDto
                {
                    Id = "subtitle", Label = "Alt yazı", Placeholder = "Örn: Bu videoda her şey var 🔥", MaxLength = 60,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 60, Y = 620, MaxWidth = 900, FontSize = 40,
                        FontName = "Inter-SemiBold", Color = "#FFD700", Alignment = "left", DropShadow = true
                    }
                }
            }
        },

        // Preset 3: Reels/TikTok Cover — Before/After
        new PresetTemplateDto
        {
            Id = "reels-before-after",
            DisplayName = "Reels/TikTok Cover — Before/After",
            Description = "Dönüşüm/karşılaştırma odaklı kapak. 1080×1920 dikey.",
            Platform = "tiktok",
            Width = 1080,
            Height = 1920,
            ModelEndpoint = "fal-ai/nano-banana",
            AspectRatioValue = "9:16",
            ImagePromptSuffix = ", split composition, dramatic contrast, transformation aesthetic, cinematic",
            PreviewImageUrl = "/img/presets/reels-before-after.jpg",
            TextFields = new List<PresetTextFieldDto>
            {
                new PresetTextFieldDto
                {
                    Id = "title", Label = "Başlık", Placeholder = "Örn: 30 GÜN SONRA", MaxLength = 30,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 540, Y = 100, MaxWidth = 1000, FontSize = 96,
                        FontName = "Inter-Bold", Color = "#FFFFFF", Alignment = "center", DropShadow = true
                    }
                },
                new PresetTextFieldDto
                {
                    Id = "subtitle", Label = "Vurgu", Placeholder = "Örn: SONUÇ ŞAŞIRTICI", MaxLength = 40,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 540, Y = 1750, MaxWidth = 1000, FontSize = 56,
                        FontName = "Inter-SemiBold", Color = "#FFD700", Alignment = "center", DropShadow = true
                    }
                }
            }
        },

        // Preset 4: Instagram Post — Ürün Fotoğrafı
        new PresetTemplateDto
        {
            Id = "ig-post-product",
            DisplayName = "Instagram Post — Ürün Fotoğrafı",
            Description = "Ürün + marka adı + kısa açıklama. 1080×1080 kare.",
            Platform = "instagram",
            Width = 1080,
            Height = 1080,
            ModelEndpoint = "fal-ai/ideogram/v3",
            AspectRatioValue = "1:1",
            ImagePromptSuffix = ", centered product composition, clean minimal background, editorial photography",
            PreviewImageUrl = "/img/presets/ig-post-product.jpg",
            TextFields = new List<PresetTextFieldDto>
            {
                new PresetTextFieldDto
                {
                    Id = "brandName", Label = "Marka Adı", Placeholder = "Örn: SELFAI", MaxLength = 20,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 540, Y = 80, MaxWidth = 1000, FontSize = 44,
                        FontName = "Inter-Bold", Color = "#FFFFFF", Alignment = "center", DropShadow = true
                    }
                },
                new PresetTextFieldDto
                {
                    Id = "description", Label = "Açıklama", Placeholder = "Örn: Doğal içerikli premium seri", MaxLength = 80,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 540, Y = 940, MaxWidth = 1000, FontSize = 36,
                        FontName = "Inter-Regular", Color = "#FFFFFF", Alignment = "center", DropShadow = true
                    }
                }
            }
        },

        // Preset 5: Facebook Ad — Sosyal Kanıt
        new PresetTemplateDto
        {
            Id = "fb-ad-social-proof",
            DisplayName = "Facebook Ad — Sosyal Kanıt",
            Description = "Görsel + müşteri yorumu + rating. 1200×630 landscape reklam.",
            Platform = "facebook",
            Width = 1200,
            Height = 630,
            ModelEndpoint = "fal-ai/ideogram/v3",
            AspectRatioValue = "16:9",
            ImagePromptSuffix = ", professional composition, corporate aesthetic, editorial style, high contrast",
            PreviewImageUrl = "/img/presets/fb-ad-social-proof.jpg",
            TextFields = new List<PresetTextFieldDto>
            {
                new PresetTextFieldDto
                {
                    Id = "quote", Label = "Müşteri Yorumu",
                    Placeholder = "Örn: \"Hayatımı değiştirdi. Kesinlikle tavsiye ederim.\"", MaxLength = 120,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 60, Y = 200, MaxWidth = 700, FontSize = 42,
                        FontName = "Inter-SemiBold", Color = "#FFFFFF", Alignment = "left", DropShadow = true
                    }
                },
                new PresetTextFieldDto
                {
                    Id = "customerName", Label = "Müşteri Adı", Placeholder = "Örn: Ayşe K., İstanbul", MaxLength = 40,
                    OverlaySpec = new TextOverlaySpec
                    {
                        X = 60, Y = 440, MaxWidth = 700, FontSize = 32,
                        FontName = "Inter-Regular", Color = "#FFD700", Alignment = "left", DropShadow = true
                    }
                }
            }
        }
    };

    public IReadOnlyList<PresetTemplateDto> GetAllPresets() => Presets;

    public PresetTemplateDto? GetPreset(string presetId) =>
        Presets.FirstOrDefault(p => p.Id.Equals(presetId, StringComparison.OrdinalIgnoreCase));
}
