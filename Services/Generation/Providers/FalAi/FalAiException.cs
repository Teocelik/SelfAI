namespace SelfAI.Services.Generation.Providers.FalAi;

/// <summary>
/// fal.ai provider katmanına özgü hata tipi. Düşük seviye HTTP/queue hataları
/// bu exception'la fırlatılır. Domain service'ler ve orchestrator'lar bunu
/// yakalayıp ServiceResult.Failure'a çevirir (ServiceResult pattern bypass edilmez).
/// </summary>
public class FalAiException : Exception
{
    /// <summary>Hatayla ilişkili HTTP status kodu (varsa).</summary>
    public int? HttpStatusCode { get; }

    /// <summary>fal.ai'dan dönen ham yanıt gövdesi (loglama/debug için).</summary>
    public string? RawResponse { get; }

    /// <summary>İlgili fal.ai request_id (varsa).</summary>
    public string? RequestId { get; }

    public FalAiException(string message) : base(message) { }

    public FalAiException(string message, int httpStatusCode, string? rawResponse = null, string? requestId = null)
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
        RawResponse = rawResponse;
        RequestId = requestId;
    }

    public FalAiException(string message, Exception innerException)
        : base(message, innerException) { }
}
