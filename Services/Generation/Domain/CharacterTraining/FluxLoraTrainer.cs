using System.Text.Json.Serialization;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.CharacterTraining;

/// <summary>
/// fal.ai flux-lora-fast-training domain service (F.M.4).
/// Tek sorumluluk: karakter LoRA training submit / status / result.
/// Provider HTTP detaylarını IFalAiClient saklar.
/// </summary>
public class FluxLoraTrainer : ICharacterTrainer
{
    // Tek-app endpoint — queue status/result URL'leri naive inşayla çözülür (sub-variant yok).
    private const string TrainingEndpoint = "fal-ai/flux-lora-fast-training";

    private readonly IFalAiClient _falAiClient;
    private readonly ILogger<FluxLoraTrainer> _logger;

    public FluxLoraTrainer(IFalAiClient falAiClient, ILogger<FluxLoraTrainer> logger)
    {
        _falAiClient = falAiClient;
        _logger = logger;
    }

    public async Task<CharacterTrainingSubmitResult> SubmitTrainingAsync(
        CharacterTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ImagesDataUrl))
            throw new ArgumentException("Yüz görselleri ZIP URL'i (images_data_url) gerekli.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.TriggerWord))
            throw new ArgumentException("Trigger word boş olamaz.", nameof(request));

        // fal.ai flux-lora-fast-training input şeması:
        // images_data_url (TEK zip URL), trigger_word, steps, is_style, create_masks.
        var payload = new
        {
            images_data_url = request.ImagesDataUrl,
            trigger_word = request.TriggerWord,
            steps = request.Steps,
            is_style = request.IsStyle,
            // Realistic karakterlerde segmentasyon maskesi özneye odaklar; style'da is_style
            // zaten segmentasyonu kapattığı için değeri önemsiz.
            create_masks = !request.IsStyle
        };

        _logger.LogInformation(
            "LoRA training submit. | TriggerWord: {TW} | IsStyle: {IsStyle} | Steps: {Steps}",
            request.TriggerWord, request.IsStyle, request.Steps);

        // Training uzun sürer (~5 dk) — SubmitAndWait DEĞİL; sadece submit, polling arka planda.
        var submitResponse = await _falAiClient.SubmitAsync(TrainingEndpoint, payload, cancellationToken);

        return new CharacterTrainingSubmitResult
        {
            RequestId = submitResponse.RequestId,
            TriggerWord = request.TriggerWord,
            SubmittedAt = DateTime.UtcNow
        };
    }

    public async Task<CharacterTrainingStatusResult> GetTrainingStatusAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        var statusResponse = await _falAiClient.GetStatusAsync(TrainingEndpoint, requestId, cancellationToken);

        var status = FalAiRequestStatusExtensions.ParseStatus(statusResponse.Status);

        string? failureReason = null;
        if (status == FalAiRequestStatus.Failed)
        {
            var logMessages = statusResponse.Logs?
                .Where(l => l != null && !string.IsNullOrEmpty(l.Message))
                .Select(l => l!.Message) ?? Enumerable.Empty<string>();
            failureReason = string.Join(" | ", logMessages);
        }

        return new CharacterTrainingStatusResult
        {
            Status = status,
            FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason
        };
    }

    public async Task<string> GetTrainingResultAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        var result = await _falAiClient.GetResultAsync<FluxLoraTrainingOutput>(
            TrainingEndpoint, requestId, cancellationToken);

        if (string.IsNullOrEmpty(result?.DiffusersLoraFile?.Url))
            throw new FalAiException("Training result LoRA URL'i (diffusers_lora_file) içermiyor.");

        return result.DiffusersLoraFile.Url;
    }
}

/// <summary>fal.ai flux-lora-fast-training output şeması.</summary>
internal class FluxLoraTrainingOutput
{
    [JsonPropertyName("diffusers_lora_file")]
    public FluxLoraFile? DiffusersLoraFile { get; set; }

    [JsonPropertyName("config_file")]
    public FluxLoraFile? ConfigFile { get; set; }
}

internal class FluxLoraFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }
}
