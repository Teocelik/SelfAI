namespace SelfAI.Services.Generation.Pricing;

// fal.ai USD maliyetini tier markup'ı ile kullanıcı kredisine çevirir.
public interface ICreditPricingService
{
    /// <summary>
    /// fal.ai USD maliyetini ve tier'ı alıp kullanıcıdan düşülecek
    /// credit miktarını hesaplar. Minimum 1 credit döner.
    /// </summary>
    int CalculateUserCredits(decimal falAiCostUsd, ModelTier tier);

    /// <summary>
    /// Verilen tier için markup multiplier'ı döner.
    /// </summary>
    decimal GetMarkupMultiplier(ModelTier tier);
}
