namespace SelfAI.Configurations;

/// <summary>
/// Tier bazlı credit pricing ayarları. fal.ai USD maliyeti + tier markup'ı ile
/// kullanıcıdan düşülecek credit hesaplanır. Markup'lar business kararıyla değişir,
/// kod değişimi gerektirmez (appsettings "CreditPricing" section'ından okunur).
/// </summary>
public class CreditPricingOptions
{
    /// <summary>1 credit'in USD karşılığı (örn. 0.005 = $0.005).</summary>
    public decimal UsdPerCredit { get; set; } = 0.005m;

    /// <summary>
    /// Bir karakter LoRA training'inin tahmini fal.ai USD maliyeti (F.M.4).
    /// CharacterLora tier markup'ı uygulanarak kullanıcı credit'i hesaplanır.
    /// Business kararıyla değişir, kod değişimi gerektirmez.
    /// </summary>
    public decimal CharacterTrainingCostUsd { get; set; } = 2.0m;

    /// <summary>ModelTier adı → markup çarpanı. Bilinmeyen tier için service 2.5x default uygular.</summary>
    public Dictionary<string, decimal> TierMarkups { get; set; } = new()
    {
        { "Fast", 4.0m },
        { "Standard", 2.5m },
        { "Premium", 2.0m },
        { "CharacterLora", 2.5m },
        { "VideoFast", 1.8m },
        { "VideoPremium", 1.5m }
    };
}
