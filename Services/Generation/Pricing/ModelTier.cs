namespace SelfAI.Services.Generation.Pricing;

// Markup tier'ları — her fal.ai endpoint bir tier'a map'lenir, credit cost buna göre hesaplanır.
public enum ModelTier
{
    Fast = 0,
    Standard = 1,
    Premium = 2,
    CharacterLora = 3,
    VideoFast = 4,
    VideoPremium = 5
}
