using Microsoft.Extensions.Options;
using SelfAI.Configurations;

namespace SelfAI.Services.Generation.Pricing;

public class CreditPricingService : ICreditPricingService
{
    private readonly CreditPricingOptions _options;
    private readonly ILogger<CreditPricingService> _logger;

    // Endpoint → Tier mapping table (F.M.5'te dinamik catalog ile değişir)
    private static readonly Dictionary<string, ModelTier> _endpointTierMap = new()
    {
        { "fal-ai/flux/schnell", ModelTier.Fast },
        { "fal-ai/flux/dev", ModelTier.Standard },
        { "fal-ai/flux-pro", ModelTier.Premium },
        { "fal-ai/recraft-v3", ModelTier.Premium },
        { "fal-ai/flux-lora", ModelTier.CharacterLora },
        { "fal-ai/flux-lora-fast-training", ModelTier.CharacterLora }
    };

    public CreditPricingService(
        IOptions<CreditPricingOptions> options,
        ILogger<CreditPricingService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int CalculateUserCredits(decimal falAiCostUsd, ModelTier tier)
    {
        if (falAiCostUsd <= 0)
        {
            _logger.LogWarning(
                "Geçersiz fal.ai maliyeti, minimum 1 credit düşülür. | Cost: {Cost} | Tier: {Tier}",
                falAiCostUsd, tier);
            return 1;
        }

        var markup = GetMarkupMultiplier(tier);
        var markedUpCost = falAiCostUsd * markup;
        var credits = (int)Math.Ceiling(markedUpCost / _options.UsdPerCredit);

        return Math.Max(1, credits);  // Minimum 1 credit
    }

    public ModelTier MapEndpointToTier(string falAiEndpointId)
    {
        if (string.IsNullOrWhiteSpace(falAiEndpointId))
        {
            _logger.LogWarning("Boş endpoint ID, Standard tier varsayılır.");
            return ModelTier.Standard;
        }

        if (_endpointTierMap.TryGetValue(falAiEndpointId, out var tier))
            return tier;

        _logger.LogWarning(
            "Bilinmeyen fal.ai endpoint, Standard tier varsayılır. | Endpoint: {Endpoint}",
            falAiEndpointId);
        return ModelTier.Standard;
    }

    public decimal GetMarkupMultiplier(ModelTier tier)
    {
        var key = tier.ToString();
        if (_options.TierMarkups.TryGetValue(key, out var multiplier))
            return multiplier;

        _logger.LogWarning(
            "Tier için markup bulunamadı, 2.5x default. | Tier: {Tier}",
            tier);
        return 2.5m;
    }
}
