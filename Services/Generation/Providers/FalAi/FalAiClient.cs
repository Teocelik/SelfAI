using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;

namespace SelfAI.Services.Generation.Providers.FalAi;

/// <summary>
/// fal.ai queue API ile konuşan provider-level HTTP client (F.M.2).
/// Tek sorumluluğu: submit / status / result HTTP iletişimi + polling.
/// İş mantığı bilmez — domain service'ler bu client'ı kullanır.
/// </summary>
public class FalAiClient : IFalAiClient
{
    private readonly HttpClient _httpClient;
    private readonly FalAiOptions _options;
    private readonly ILogger<FalAiClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public FalAiClient(
        HttpClient httpClient,
        IOptions<FalAiOptions> options,
        ILogger<FalAiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (_httpClient.BaseAddress == null)
            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        // Auth: Authorization: Key <api-key>
        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_options.ApiKey}");
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.DefaultTimeoutSeconds);
    }

    public async Task<FalAiQueueSubmitResponse> SubmitAsync(
        string modelEndpoint,
        object payload,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelEndpoint))
            throw new ArgumentException("Model endpoint boş olamaz.", nameof(modelEndpoint));

        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        var url = modelEndpoint.TrimStart('/');

        _logger.LogInformation(
            "fal.ai queue submit. | Model: {Model} | PayloadType: {PayloadType}",
            modelEndpoint, payload.GetType().Name);

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, _jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "fal.ai submit hatası. | StatusCode: {StatusCode} | Response: {Response}",
                    (int)response.StatusCode, rawError);

                throw new FalAiException(
                    $"fal.ai submit başarısız (HTTP {(int)response.StatusCode})",
                    (int)response.StatusCode,
                    rawError);
            }

            var result = await response.Content.ReadFromJsonAsync<FalAiQueueSubmitResponse>(_jsonOptions, cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.RequestId))
                throw new FalAiException("fal.ai submit response geçersiz (request_id yok).");

            _logger.LogInformation(
                "fal.ai queue accepted. | Model: {Model} | RequestId: {RequestId} | Status: {Status}",
                modelEndpoint, result.RequestId, result.Status);

            return result;
        }
        catch (FalAiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fal.ai submit beklenmedik hata. | Model: {Model}", modelEndpoint);
            throw new FalAiException("fal.ai submit başarısız.", ex);
        }
    }

    /// <summary>
    /// Verilen modelEndpoint + requestId'den status URL'i naive olarak inşa edip durum sorgular.
    /// </summary>
    /// <remarks>
    /// UYARI: fal.ai bazı modellerde submit endpoint ile status endpoint farklı
    /// namespace kullanır (örn. fal-ai/flux/schnell submit eder, fal-ai/flux/requests/...
    /// status verir). Bu metot naive URL inşası yapar, hiyerarşik endpoint'lerde
    /// HTTP 405 dönebilir. Güvenli yol: SubmitAndWaitAsync kullan (submit response'un
    /// URL'lerini doğrudan tüketir). Bu metot F.M.3'te refactor edilecek.
    /// </remarks>
    public async Task<FalAiQueueStatusResponse> GetStatusAsync(
        string modelEndpoint,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelEndpoint))
            throw new ArgumentException("Model endpoint boş olamaz.", nameof(modelEndpoint));

        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID boş olamaz.", nameof(requestId));

        // logs=1 → FAILED durumunda hata loglarını yanıta dahil eder (Logs alanı dolar).
        var url = $"{modelEndpoint.TrimStart('/').TrimEnd('/')}/requests/{requestId}/status?logs=1";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "fal.ai status hatası. | RequestId: {RequestId} | StatusCode: {StatusCode}",
                    requestId, (int)response.StatusCode);

                throw new FalAiException(
                    $"fal.ai status check başarısız (HTTP {(int)response.StatusCode})",
                    (int)response.StatusCode,
                    rawError,
                    requestId);
            }

            var result = await response.Content.ReadFromJsonAsync<FalAiQueueStatusResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new FalAiException("fal.ai status response boş.", 200, null, requestId);
        }
        catch (FalAiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fal.ai status beklenmedik hata. | RequestId: {RequestId}", requestId);
            throw new FalAiException("fal.ai status check başarısız.", ex);
        }
    }

    /// <summary>
    /// Verilen modelEndpoint + requestId'den result URL'i naive olarak inşa edip sonucu çeker.
    /// </summary>
    /// <remarks>
    /// UYARI: fal.ai bazı modellerde submit endpoint ile result endpoint farklı
    /// namespace kullanır (örn. fal-ai/flux/schnell submit eder, fal-ai/flux/requests/...
    /// result verir). Bu metot naive URL inşası yapar, hiyerarşik endpoint'lerde
    /// HTTP 405 dönebilir. Güvenli yol: SubmitAndWaitAsync kullan (submit response'un
    /// URL'lerini doğrudan tüketir). Bu metot F.M.3'te refactor edilecek.
    /// </remarks>
    public async Task<T> GetResultAsync<T>(
        string modelEndpoint,
        string requestId,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(modelEndpoint))
            throw new ArgumentException("Model endpoint boş olamaz.", nameof(modelEndpoint));

        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID boş olamaz.", nameof(requestId));

        var url = $"{modelEndpoint.TrimStart('/').TrimEnd('/')}/requests/{requestId}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "fal.ai result fetch hatası. | RequestId: {RequestId} | StatusCode: {StatusCode} | Response: {Response}",
                    requestId, (int)response.StatusCode, rawError);

                throw new FalAiException(
                    $"fal.ai result fetch başarısız (HTTP {(int)response.StatusCode})",
                    (int)response.StatusCode,
                    rawError,
                    requestId);
            }

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            return result ?? throw new FalAiException("fal.ai result deserialize edilemedi.", 200, null, requestId);
        }
        catch (FalAiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fal.ai result fetch beklenmedik hata. | RequestId: {RequestId}", requestId);
            throw new FalAiException("fal.ai result fetch başarısız.", ex);
        }
    }

    public async Task<T> SubmitAndWaitAsync<T>(
        string modelEndpoint,
        object payload,
        CancellationToken cancellationToken = default) where T : class
    {
        var submitResponse = await SubmitAsync(modelEndpoint, payload, cancellationToken);
        var requestId = submitResponse.RequestId;

        // fal.ai submit response'unda absolute URL'ler döner.
        // Hiyerarşik endpoint'ler için (örn. fal-ai/flux/schnell) status URL
        // farklı namespace kullanır (fal-ai/flux). Submit response'un URL'leri
        // authoritative — kendi URL inşası yapmıyoruz.
        var statusUrl = submitResponse.StatusUrl;
        var responseUrl = submitResponse.ResponseUrl;

        if (string.IsNullOrEmpty(statusUrl) || string.IsNullOrEmpty(responseUrl))
        {
            _logger.LogError(
                "fal.ai submit response'da URL'ler eksik. | RequestId: {RequestId} | StatusUrl: {StatusUrl} | ResponseUrl: {ResponseUrl}",
                requestId, statusUrl ?? "(null)", responseUrl ?? "(null)");
            throw new FalAiException(
                "fal.ai submit response geçersiz (status_url veya response_url eksik).",
                0, null, requestId);
        }

        _logger.LogInformation(
            "fal.ai polling başladı. | Model: {Model} | RequestId: {RequestId} | StatusUrl: {StatusUrl}",
            modelEndpoint, requestId, statusUrl);

        for (int attempt = 1; attempt <= _options.MaxPollingAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(_options.PollingIntervalMs, cancellationToken);

            FalAiQueueStatusResponse? statusResponse;
            try
            {
                statusResponse = await GetStatusByAbsoluteUrlAsync(statusUrl, requestId, cancellationToken);
            }
            catch (FalAiException ex) when (ex.HttpStatusCode is 502 or 503 or 504)
            {
                // Geçici sunucu hataları — bir sonraki polling'de tekrar dene
                _logger.LogWarning(
                    "fal.ai geçici status hatası, polling devam. | RequestId: {RequestId} | Attempt: {Attempt} | StatusCode: {StatusCode}",
                    requestId, attempt, ex.HttpStatusCode);
                continue;
            }

            var status = FalAiRequestStatusExtensions.ParseStatus(statusResponse.Status);

            if (status == FalAiRequestStatus.Completed)
            {
                _logger.LogInformation(
                    "fal.ai COMPLETED. | RequestId: {RequestId} | Attempt: {Attempt}",
                    requestId, attempt);

                return await GetResultByAbsoluteUrlAsync<T>(responseUrl, requestId, cancellationToken);
            }

            if (status == FalAiRequestStatus.Failed)
            {
                var logMessages = statusResponse.Logs?
                    .Where(l => l != null)
                    .Select(l => l.Message)
                    .Where(m => !string.IsNullOrEmpty(m)) ?? Enumerable.Empty<string>();
                var combinedLog = string.Join(" | ", logMessages);

                _logger.LogError(
                    "fal.ai FAILED. | RequestId: {RequestId} | Logs: {Logs}",
                    requestId, combinedLog);

                throw new FalAiException(
                    $"fal.ai generation FAILED. Logs: {combinedLog}",
                    0,
                    combinedLog,
                    requestId);
            }

            // IN_QUEUE veya IN_PROGRESS — devam et
            if (attempt % 5 == 0)  // Her 5 attempt'ta bir log (gürültü azaltma)
            {
                _logger.LogInformation(
                    "fal.ai bekliyor. | RequestId: {RequestId} | Status: {Status} | Attempt: {Attempt}",
                    requestId, status, attempt);
            }
        }

        _logger.LogError(
            "fal.ai polling timeout. | RequestId: {RequestId} | MaxAttempts: {MaxAttempts}",
            requestId, _options.MaxPollingAttempts);

        throw new FalAiException(
            $"fal.ai polling timeout (request hâlâ COMPLETED değil). RequestId: {requestId}",
            0,
            null,
            requestId);
    }

    /// <summary>
    /// fal.ai submit response'unun absolute status_url'ini doğrudan kullanarak durum sorgular.
    /// Naive URL inşası yapmaz — hiyerarşik endpoint'lerde HTTP 405 sorununu önler.
    /// </summary>
    private async Task<FalAiQueueStatusResponse> GetStatusByAbsoluteUrlAsync(
        string absoluteUrl,
        string requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(absoluteUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "fal.ai status hatası. | RequestId: {RequestId} | StatusCode: {StatusCode} | Url: {Url}",
                    requestId, (int)response.StatusCode, absoluteUrl);

                throw new FalAiException(
                    $"fal.ai status check başarısız (HTTP {(int)response.StatusCode})",
                    (int)response.StatusCode, rawError, requestId);
            }

            var result = await response.Content.ReadFromJsonAsync<FalAiQueueStatusResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new FalAiException("fal.ai status response boş.", 200, null, requestId);
        }
        catch (FalAiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fal.ai status beklenmedik hata. | RequestId: {RequestId}", requestId);
            throw new FalAiException("fal.ai status check başarısız.", ex);
        }
    }

    /// <summary>
    /// fal.ai submit response'unun absolute response_url'ini doğrudan kullanarak sonucu çeker.
    /// Naive URL inşası yapmaz — hiyerarşik endpoint'lerde HTTP 405 sorununu önler.
    /// </summary>
    private async Task<T> GetResultByAbsoluteUrlAsync<T>(
        string absoluteUrl,
        string requestId,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            var response = await _httpClient.GetAsync(absoluteUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var rawError = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "fal.ai result fetch hatası. | RequestId: {RequestId} | StatusCode: {StatusCode} | Url: {Url} | Response: {Response}",
                    requestId, (int)response.StatusCode, absoluteUrl, rawError);

                throw new FalAiException(
                    $"fal.ai result fetch başarısız (HTTP {(int)response.StatusCode})",
                    (int)response.StatusCode, rawError, requestId);
            }

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            return result ?? throw new FalAiException("fal.ai result deserialize edilemedi.", 200, null, requestId);
        }
        catch (FalAiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fal.ai result fetch beklenmedik hata. | RequestId: {RequestId}", requestId);
            throw new FalAiException("fal.ai result fetch başarısız.", ex);
        }
    }
}
