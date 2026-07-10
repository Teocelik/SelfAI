using SelfAI.DTOs.Templates;
using SelfAI.Models;
using SelfAI.Services.Generation.Orchestrators;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Templates üretim koordinatörü (F.M.10a). İnce katman: format lookup + prompt
/// suffix birleştirme yapar, ardından mevcut <see cref="Generation.Abstractions.IGenerationOrchestrator"/>
/// pipeline'ına delege eder (kredi/log/refund/SignalR yeniden yazılmaz, reuse edilir).
/// </summary>
public interface ITemplateGenerationOrchestrator
{
    /// <summary>
    /// Format'a göre üretimi başlatır. Fire-and-forget — sonuç SignalR "GenerationUpdate"
    /// ile gelir; response yalnızca tracking ID + düşülen kredi içerir.
    /// </summary>
    Task<ServiceResult<GenerationStartedResponse>> GenerateAsync(
        TemplateGenerationRequest request,
        Guid userId,
        string firebaseUid,
        string? signalRConnectionId,
        CancellationToken cancellationToken = default);
}
