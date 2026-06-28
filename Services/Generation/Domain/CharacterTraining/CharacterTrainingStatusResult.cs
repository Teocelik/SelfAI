using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.CharacterTraining;

/// <summary>fal.ai training durum sonucu. Failed ise FailureReason loglardan derlenir.</summary>
public class CharacterTrainingStatusResult
{
    public FalAiRequestStatus Status { get; set; }
    public string? FailureReason { get; set; }
}
