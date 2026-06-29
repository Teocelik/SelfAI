using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;
using System.Net.Http.Json;

namespace SelfAI.Services.Generation.Providers.FalAi;

/// <summary>
/// fal.ai unified model list endpoint client (F.M.5).
/// NOT: Catalog endpoint queue.fal.run değil, api.fal.ai'da. Bu yüzden BaseUrl
/// burada sabit; FalAiOptions sadece ApiKey için kullanılır.
/// Sadece HTTP/pagination — iş kuralı yok (Infrastructure).
/// </summary>
public class FalAiModelCatalogClient : IFalAiModelCatalogClient
{
    private const string CatalogBaseUrl = "https://api.fal.ai/v1";
    private const int PageSize = 100;
    private const int MaxPages = 20;  // Safety cap — 20 × 100 = 2000 model

    private readonly HttpClient _httpClient;
    private readonly FalAiOptions _options;
    private readonly ILogger<FalAiModelCatalogClient> _logger;

    public FalAiModelCatalogClient(
        HttpClient httpClient,
        IOptions<FalAiOptions> options,
        ILogger<FalAiModelCatalogClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<FalAiModelItem>> ListAllAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var allModels = new List<FalAiModelItem>();
        string? cursor = null;
        int page = 0;

        do
        {
            page++;
            if (page > MaxPages)
            {
                _logger.LogWarning(
                    "fal.ai catalog pagination cap'e ulaştı. | MaxPages: {Max}",
                    MaxPages);
                break;
            }

            var url = BuildUrl(cursor, category);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Key {_options.ApiKey}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "fal.ai catalog fetch hatası. | Page: {Page} | StatusCode: {StatusCode} | Error: {Error}",
                    page, (int)response.StatusCode, error);
                break;
            }

            var data = await response.Content.ReadFromJsonAsync<FalAiModelListResponse>(
                cancellationToken: cancellationToken);

            if (data?.Models == null)
                break;

            allModels.AddRange(data.Models);
            cursor = data.NextCursor;

            if (!data.HasMore || string.IsNullOrEmpty(cursor))
                break;
        } while (true);

        _logger.LogInformation(
            "fal.ai catalog fetch tamamlandı. | Category: {Category} | TotalModels: {Count} | Pages: {Pages}",
            category ?? "all", allModels.Count, page);

        return allModels;
    }

    private static string BuildUrl(string? cursor, string? category)
    {
        var url = $"{CatalogBaseUrl}/models?limit={PageSize}&status=active";
        if (!string.IsNullOrEmpty(cursor))
            url += $"&cursor={Uri.EscapeDataString(cursor)}";
        if (!string.IsNullOrEmpty(category))
            url += $"&category={Uri.EscapeDataString(category)}";
        return url;
    }
}
