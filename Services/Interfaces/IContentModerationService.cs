using SelfAI.DTOs.Moderation;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Kullanıcı prompt'larını statik keyword blacklist'e karşı denetleyen servis (F.8).
/// Beta launch öncesi Stripe/MoR hesap donma riskini azaltmak için 1. koruma katmanı.
/// (2. katman fal.ai safety checker, 3. katman ToS.)
/// </summary>
public interface IContentModerationService
{
    /// <summary>
    /// Prompt'u blacklist'e karşı kontrol eder. Eşleşme varsa
    /// <see cref="ModerationResult.IsBlocked"/> = true döner.
    /// </summary>
    ModerationResult CheckPrompt(string prompt);
}
