namespace SelfAI.Services.Generation.Domain.Catalog;

/// <summary>
/// Model-spesifik default payload sağlayıcısı (F.M.5). Stateless singleton.
/// Common alanlar (prompt/image_size/num_images) DynamicImageGenerator'da set
/// edilir; bu sınıf yalnızca modele özel ekstra alanları döner.
/// </summary>
public class ModelDefaultProvider : IModelDefaultProvider
{
    private static readonly Dictionary<string, Dictionary<string, object>> _defaults = new()
    {
        ["fal-ai/flux/dev"] = new()
        {
            ["num_inference_steps"] = 28,
            ["guidance_scale"] = 3.5m,
            ["enable_safety_checker"] = true
        },
        ["fal-ai/flux/schnell"] = new()
        {
            ["num_inference_steps"] = 4,
            ["enable_safety_checker"] = true
        },
        ["fal-ai/flux-pro/v1.1"] = new()
        {
            ["enable_safety_checker"] = true
        },
        ["fal-ai/recraft/v3/text-to-image"] = new()
        {
            ["style"] = "realistic_image"
        },
        ["fal-ai/recraft-20b"] = new()
        {
            ["style"] = "realistic_image"
        },
        ["fal-ai/bytedance/seedream/v4/text-to-image"] = new(),
        ["fal-ai/nano-banana"] = new(),
        ["fal-ai/gpt-image-1.5"] = new(),
        ["fal-ai/ideogram/v3"] = new()
        {
            ["style"] = "AUTO"
        }
    };

    public Dictionary<string, object> GetDefaultsFor(string endpointId)
    {
        if (!string.IsNullOrEmpty(endpointId) && _defaults.TryGetValue(endpointId, out var defaults))
            return new Dictionary<string, object>(defaults);  // Defensive copy

        return new Dictionary<string, object>();
    }
}
