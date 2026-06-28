using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Services.Generation.Abstractions;

namespace SelfAI.Services.Generation.Providers.FalAi;

/// <summary>
/// fal.ai storage upload client (F.M.4). İki adımlı akış:
/// 1) POST initiate → { upload_url, file_url }
/// 2) PUT upload_url (pre-signed) ile binary içerik gönderilir
/// file_url public döner. Tek sorumluluk: storage HTTP iletişimi.
/// </summary>
public class FalAiStorageClient : IFalAiStorageClient
{
    // fal.ai resmi storage initiate endpoint'i. Değişirse fal.ai docs'a bakılır (F.M.7).
    private const string StorageInitiateUrl = "https://rest.alpha.fal.ai/storage/upload/initiate";

    private readonly HttpClient _httpClient;
    private readonly FalAiOptions _options;
    private readonly ILogger<FalAiStorageClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FalAiStorageClient(
        HttpClient httpClient,
        IOptions<FalAiOptions> options,
        ILogger<FalAiStorageClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("Dosya stream'i boş.", nameof(fileStream));

        _logger.LogInformation(
            "fal.ai storage upload başladı. | FileName: {FileName} | Size: {Size} | ContentType: {ContentType}",
            fileName, fileStream.Length, contentType);

        // ── Step 1: Initiate upload ──
        var initiateRequest = new HttpRequestMessage(HttpMethod.Post, StorageInitiateUrl);
        initiateRequest.Headers.Add("Authorization", $"Key {_options.ApiKey}");
        initiateRequest.Content = JsonContent.Create(new
        {
            content_type = contentType,
            file_name = fileName
        });

        using var initiateResponse = await _httpClient.SendAsync(initiateRequest, cancellationToken);
        if (!initiateResponse.IsSuccessStatusCode)
        {
            var error = await initiateResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "fal.ai storage initiate hatası. | StatusCode: {StatusCode} | Response: {Response}",
                (int)initiateResponse.StatusCode, error);
            throw new FalAiException(
                $"fal.ai storage initiate başarısız (HTTP {(int)initiateResponse.StatusCode})",
                (int)initiateResponse.StatusCode, error);
        }

        var initiateResult = await initiateResponse.Content
            .ReadFromJsonAsync<FalAiStorageInitiateResponse>(_jsonOptions, cancellationToken);

        if (initiateResult == null
            || string.IsNullOrEmpty(initiateResult.UploadUrl)
            || string.IsNullOrEmpty(initiateResult.FileUrl))
        {
            throw new FalAiException("fal.ai storage initiate response geçersiz (upload_url/file_url yok).");
        }

        // ── Step 2: PUT actual file (pre-signed upload_url, auth header gerekmez) ──
        fileStream.Position = 0;
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Put, initiateResult.UploadUrl);
        uploadRequest.Content = new StreamContent(fileStream);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var uploadResponse = await _httpClient.SendAsync(uploadRequest, cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            var error = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "fal.ai storage PUT hatası. | StatusCode: {StatusCode} | Response: {Response}",
                (int)uploadResponse.StatusCode, error);
            throw new FalAiException(
                $"fal.ai storage upload başarısız (HTTP {(int)uploadResponse.StatusCode})",
                (int)uploadResponse.StatusCode, error);
        }

        _logger.LogInformation(
            "fal.ai storage upload tamamlandı. | FileUrl: {FileUrl}",
            initiateResult.FileUrl);

        return initiateResult.FileUrl;
    }
}

/// <summary>fal.ai storage initiate yanıtı.</summary>
internal class FalAiStorageInitiateResponse
{
    [JsonPropertyName("upload_url")]
    public string UploadUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;
}
