using SelfAI.Models;

namespace SelfAI.Services.Generation.Domain.Music;

/// <summary>
/// Domain-level müzik generator (F.M.10c). Tek sorumluluğu: bir fal.ai music endpoint'ine
/// submit + polling yapıp mp3/m4a URL'i döndürmek. Provider HTTP detayları IFalAiClient'ta.
/// Kredi/log/SignalR bilmez — orchestrator koordine eder.
/// </summary>
public interface IMusicGenerator
{
    /// <summary>
    /// Music generation submit + polling. Başarısızlıkta exception fırlatmaz;
    /// ServiceResult.Failure döner (IFalAiClient'ın FalAiException'ı burada yakalanır).
    /// </summary>
    Task<ServiceResult<MusicGenerationOutput>> GenerateAsync(
        string endpointId,
        MusicGenerationInput input,
        CancellationToken cancellationToken = default);
}

/// <summary>Domain-level müzik üretim girdisi. Endpoint-spesifik payload generator'da kurulur.</summary>
public class MusicGenerationInput
{
    public string Prompt { get; set; } = string.Empty;
    public string? Lyrics { get; set; }
    public int DurationSeconds { get; set; }
}

/// <summary>Domain-level müzik üretim çıktısı. AudioUrl fal.ai geçici CDN URL'i (R2'a taşınır).</summary>
public class MusicGenerationOutput
{
    public string AudioUrl { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string? ContentType { get; set; }  // audio/mpeg, audio/mp4, ...
    public long? FileSizeBytes { get; set; }
}
