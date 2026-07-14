using System.ComponentModel.DataAnnotations;

namespace SelfAI.DTOs.Music;

/// <summary>
/// Müzik üretim isteği (F.M.10c). "backing" → Sonilo enstrümantal, "song" → MiniMax vokal.
/// Süre backing/song moduna göre orchestrator'da whitelist ile doğrulanır.
/// </summary>
public class MusicGenerationRequest
{
    /// <summary>"backing" (enstrümantal) veya "song" (vokal + lyrics).</summary>
    [Required]
    public string Mode { get; set; } = "backing";

    /// <summary>Müzik prompt'u. Örn: "upbeat electronic dance, high energy, tropical vibes".</summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string MusicPrompt { get; set; } = string.Empty;

    /// <summary>Şarkı sözleri (opsiyonel, yalnızca "song"). Boşsa MiniMax otomatik üretir.</summary>
    [StringLength(2000)]
    public string? Lyrics { get; set; }

    /// <summary>
    /// Süre (saniye). Backing: 15/30/60, Song: 60/90/180.
    /// Not: MiniMax (song) süreyi kendi belirler — bu değer song modunda yalnızca
    /// görüntüleme metadata'sıdır; Sonilo (backing) için gerçek süre parametresidir.
    /// </summary>
    [Required]
    [Range(15, 180)]
    public int DurationSeconds { get; set; }

    /// <summary>Albüm kapağı aspect ratio. "1:1" / "9:16" / "16:9" / "4:5".</summary>
    [Required]
    public string CoverAspectRatio { get; set; } = "1:1";

    /// <summary>SignalR connection ID (progress push için). Controller header'dan doldurur.</summary>
    public string? SignalRConnectionId { get; set; }
}
