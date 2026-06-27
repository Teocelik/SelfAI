using SelfAI.Models;
using SelfAI.Services.Generation.Orchestrators;

namespace SelfAI.Services.Generation.Abstractions;

// Yüksek seviye generation iş akışı: model seçimi, kredi düşme, history, downstream çağrılar.
public interface IGenerationOrchestrator
{
    /// <summary>
    /// Studio'dan gelen generation isteğini koordine eder:
    /// 1. Model lookup + tier hesabı
    /// 2. Kredi yeterliliği kontrolü + pre-charge
    /// 3. fal.ai üzerinden generation (fire-and-forget background)
    /// 4. Sonuç → DB history + SignalR notification
    /// 5. Hata → kredi refund
    /// </summary>
    /// <param name="userId">AppUser.Id (kredi/DB için).</param>
    /// <param name="firebaseUid">SignalR Clients.User() fallback routing için.</param>
    /// <param name="signalRConnectionId">X-SignalR-ConnectionId — primary push hedefi.</param>
    Task<ServiceResult<GenerationStartedResponse>> StartGenerationAsync(
        StartGenerationRequest request,
        Guid userId,
        string firebaseUid,
        string? signalRConnectionId,
        CancellationToken cancellationToken = default);
}
