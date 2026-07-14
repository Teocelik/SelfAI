using SelfAI.DTOs.Music;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Müzik üretim iş akışı koordinatörü (F.M.10c). Model seçimi, süre doğrulama, kombine
/// kredi düşme (müzik + kapak tek işlem), log, R2 upload, albüm kapağı üretimi ve SignalR
/// event push burada. Controller yalnızca HTTP transport.
/// </summary>
public interface IMusicGenerationOrchestrator
{
    /// <summary>
    /// Backing track (Sonilo) veya full song (MiniMax) üretir + otomatik albüm kapağı (Ideogram V3).
    /// Kredi düşme/iade, log, SignalR event push dahil. Uzun süren üretim background task'ta.
    /// </summary>
    Task<ServiceResult<MusicGenerationStartedDto>> StartGenerationAsync(
        MusicGenerationRequest request,
        Guid userId,
        string firebaseUid,
        CancellationToken cancellationToken = default);
}
