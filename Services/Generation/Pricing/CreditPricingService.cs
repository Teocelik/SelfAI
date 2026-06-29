using Microsoft.Extensions.Options;
using SelfAI.Configurations;

namespace SelfAI.Services.Generation.Pricing;

public class CreditPricingService : ICreditPricingService
{
    private readonly CreditPricingOptions _options;
    private readonly ILogger<CreditPricingService> _logger;

    // F.M.5: Endpoint → Tier hardcoded map kaldırıldı. Tier resolution artık
    // ICatalogTierResolver'ın sorumluluğu (DB'deki ModelCatalogEntry.Tier'dan okunur).

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
