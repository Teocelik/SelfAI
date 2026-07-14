using System.Text.Json.Serialization;

namespace SelfAI.Services.Generation.Providers.FalAi.Models;

/// <summary>
/// fal.ai music (Sonilo v1.1 + MiniMax Music v2.6) result yanıtının tip-güvenli karşılığı.
/// Her iki model de tek "audio" File object'i döner (Sonilo ayrıca "audios" array döner ama
/// birincil çıktı "audio"). snake_case alanlar JsonPropertyName ile eşlenir.
///
/// Doğrulanmış şema (fal.ai docs, F.M.10c):
///   Sonilo  → audio.content_type = "audio/mp4"  (m4a / AAC)
///   MiniMax → audio.content_type = "audio/mpeg" (mp3, audio_setting.format default)
/// </summary>
public class FalAiMusicResponse
{
    [JsonPropertyName("audio")]
    public FalAiAudioFile? Audio { get; set; }

    [JsonPropertyName("audios")]
    public List<FalAiAudioFile>? Audios { get; set; }
}

public class FalAiAudioFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("file_size")]
    public long? FileSize { get; set; }
}
