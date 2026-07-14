using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Domain.Music;

/// <summary>
/// Generic text-to-music generator (F.M.10c). Endpoint string parametre olarak verilir;
/// payload model-spesifik kurulur. F.M.10c'de yalnızca Sonilo V1.1 + MiniMax Music v2.6 destekli.
///
/// IFalAiClient.SubmitAndWaitAsync başarısızlıkta FalAiException FIRLATIR (ServiceResult DÖNMEZ),
/// bu yüzden burada try/catch ile sarılıp ServiceResult'a çevrilir (orchestrator ServiceResult bekler).
///
/// Doğrulanmış fal.ai şeması (docs):
///   Sonilo  (sonilo/v1.1/text-to-music) → input: { prompt, duration }; output: { audio: { url, content_type="audio/mp4", ... } }
///   MiniMax (fal-ai/minimax-music/v2.6)  → input: { prompt, lyrics?, lyrics_optimizer?, is_instrumental }; DURATION YOK
///                                          output: { audio: { url, content_type, ... } }
/// </summary>
public class DynamicMusicGenerator : IMusicGenerator
{
    public const string SoniloEndpoint = "sonilo/v1.1/text-to-music";
    public const string MiniMaxEndpoint = "fal-ai/minimax-music/v2.6";

    private readonly IFalAiClient _falAiClient;
    private readonly ILogger<DynamicMusicGenerator> _logger;

    public DynamicMusicGenerator(
        IFalAiClient falAiClient,
        ILogger<DynamicMusicGenerator> logger)
    {
        _falAiClient = falAiClient;
        _logger = logger;
    }

    public async Task<ServiceResult<MusicGenerationOutput>> GenerateAsync(
        string endpointId,
        MusicGenerationInput input,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> payload;
        try
        {
            payload = BuildPayload(endpointId, input);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Music payload kurulamadı. | Endpoint: {Endpoint}", endpointId);
            return ServiceResult<MusicGenerationOutput>.Failure(
                "Desteklenmeyen müzik modeli.", 400);
        }

        _logger.LogInformation(
            "Music generation başladı. | Endpoint: {Endpoint} | Duration: {Dur}s | HasLyrics: {HasLyrics}",
            endpointId, input.DurationSeconds, !string.IsNullOrWhiteSpace(input.Lyrics));

        try
        {
            var response = await _falAiClient.SubmitAndWaitAsync<FalAiMusicResponse>(
                endpointId, payload, cancellationToken);

            // Birincil çıktı "audio"; Sonilo ayrıca "audios" array döner → ilkine düş.
            var audio = response?.Audio
                        ?? (response?.Audios != null && response.Audios.Count > 0 ? response.Audios[0] : null);

            if (audio == null || string.IsNullOrWhiteSpace(audio.Url))
            {
                _logger.LogWarning(
                    "Music response'ta audio URL yok. | Endpoint: {Endpoint}", endpointId);
                return ServiceResult<MusicGenerationOutput>.Failure(
                    "Müzik çıktısı alınamadı.", 502);
            }

            var output = new MusicGenerationOutput
            {
                AudioUrl = audio.Url,
                DurationSeconds = input.DurationSeconds,
                ContentType = audio.ContentType,
                FileSizeBytes = audio.FileSize
            };

            _logger.LogInformation(
                "Music generation başarılı. | Endpoint: {Endpoint} | ContentType: {Ct} | Url: {Url}",
                endpointId, output.ContentType, output.AudioUrl);

            return ServiceResult<MusicGenerationOutput>.Success(output);
        }
        catch (Exception ex)
        {
            // FalAiException (submit/polling/timeout) dahil tüm hatalar burada yakalanır.
            _logger.LogError(ex, "Music generation hatası. | Endpoint: {Endpoint}", endpointId);
            return ServiceResult<MusicGenerationOutput>.Failure(
                "Müzik üretimi sırasında bir hata oluştu.", 502);
        }
    }

    // Endpoint-spesifik payload — fal.ai doğrulanmış şemasına göre.
    private static Dictionary<string, object> BuildPayload(string endpointId, MusicGenerationInput input)
    {
        switch (endpointId)
        {
            case SoniloEndpoint:
                // Sonilo: prompt + duration (num_samples default 1). output_format şemada yok.
                return new Dictionary<string, object>
                {
                    ["prompt"] = input.Prompt,
                    ["duration"] = input.DurationSeconds
                };

            case MiniMaxEndpoint:
            {
                // MiniMax: DURATION parametresi YOK (uzunluğu kendi belirler). Song modu → vokal.
                // Lyrics boşsa lyrics_optimizer=true ile otomatik söz üretir.
                var hasLyrics = !string.IsNullOrWhiteSpace(input.Lyrics);
                var payload = new Dictionary<string, object>
                {
                    ["prompt"] = input.Prompt,
                    ["is_instrumental"] = false
                };
                if (hasLyrics)
                    payload["lyrics"] = input.Lyrics!;
                else
                    payload["lyrics_optimizer"] = true;
                return payload;
            }

            default:
                throw new InvalidOperationException(
                    $"Bilinmeyen music endpoint: {endpointId}. " +
                    "F.M.10c'de sadece Sonilo V1.1 ve MiniMax Music v2.6 destekli.");
        }
    }
}
