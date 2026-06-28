using SelfAI.Services.Generation.Domain.CharacterTraining;

namespace SelfAI.Services.Generation.Abstractions;

/// <summary>
/// Karakter LoRA training iş mantığı (F.M.4). fal.ai flux-lora-fast-training
/// endpoint'ini sarmalar. Training async — submit + polling + result fetch ayrı adımlar.
/// </summary>
public interface ICharacterTrainer
{
    /// <summary>
    /// fal.ai LoRA training submit eder. request_id döner, durum polling ile takip edilir.
    /// </summary>
    Task<CharacterTrainingSubmitResult> SubmitTrainingAsync(
        CharacterTrainingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Training durumunu kontrol eder (IN_QUEUE / IN_PROGRESS / COMPLETED / FAILED).</summary>
    Task<CharacterTrainingStatusResult> GetTrainingStatusAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>COMPLETED training'in LoRA model URL'ini fetch eder.</summary>
    Task<string> GetTrainingResultAsync(
        string requestId,
        CancellationToken cancellationToken = default);
}
