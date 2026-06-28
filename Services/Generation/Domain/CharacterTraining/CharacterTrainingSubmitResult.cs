namespace SelfAI.Services.Generation.Domain.CharacterTraining;

/// <summary>fal.ai training submit sonucu — request_id ile durum sorgulanır.</summary>
public class CharacterTrainingSubmitResult
{
    public string RequestId { get; set; } = string.Empty;
    public string TriggerWord { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
