using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Abstractions;

/// <summary>
/// fal.ai provider-level HTTP client. Düşük seviye queue/polling iletişimi.
/// Domain service'ler (FluxDevGenerator vb.) bu abstraction üzerinden çalışır,
/// concrete FalAiClient'a doğrudan bağlanmaz (Dependency Inversion).
/// </summary>
public interface IFalAiClient
{
    /// <summary>
    /// fal.ai model endpoint'ine asenkron generation request submit eder.
    /// Queue'ya eklenir, request_id döner. Henüz tamamlanmamıştır.
    /// </summary>
    /// <param name="modelEndpoint">örn. "fal-ai/flux/schnell"</param>
    /// <param name="payload">Model-spesifik input (caller serialize eder)</param>
    Task<FalAiQueueSubmitResponse> SubmitAsync(
        string modelEndpoint,
        object payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Request'in mevcut durumunu kontrol eder. Tamamlandıysa COMPLETED,
    /// hata varsa FAILED döner.
    /// </summary>
    Task<FalAiQueueStatusResponse> GetStatusAsync(
        string modelEndpoint,
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tamamlanmış request'in result'unu fetch eder. Generic T tipinde
    /// deserialize edilir (caller model-spesifik DTO verir).
    /// COMPLETED olmadan çağrılırsa exception fırlatır.
    /// </summary>
    Task<T> GetResultAsync<T>(
        string modelEndpoint,
        string requestId,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// SubmitAsync + polling + GetResultAsync birleşik akışı.
    /// COMPLETED olana kadar bekler veya FAILED/timeout durumunda
    /// FalAiException fırlatır.
    /// </summary>
    Task<T> SubmitAndWaitAsync<T>(
        string modelEndpoint,
        object payload,
        CancellationToken cancellationToken = default) where T : class;
}
