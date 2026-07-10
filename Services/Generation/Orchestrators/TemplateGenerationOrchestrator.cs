using SelfAI.DTOs.Templates;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Templates üretim koordinatörü (F.M.10a). Format lookup + prompt suffix birleştirme
/// yapar, sonra mevcut <see cref="IGenerationOrchestrator"/> pipeline'ına delege eder.
///
/// Kasıtlı olarak İNCE: kredi düşme, history, refund, background task ve SignalR push
/// mantığı zaten <see cref="GenerationOrchestrator"/>'da var — burada duplike edilmez.
/// Templates'in tek katkısı format→StartGenerationRequest çevirisidir.
/// </summary>
public class TemplateGenerationOrchestrator : ITemplateGenerationOrchestrator
{
    private readonly ITemplateCatalogService _catalog;
    private readonly IGenerationOrchestrator _generationOrchestrator;
    private readonly ILogger<TemplateGenerationOrchestrator> _logger;

    public TemplateGenerationOrchestrator(
        ITemplateCatalogService catalog,
        IGenerationOrchestrator generationOrchestrator,
        ILogger<TemplateGenerationOrchestrator> logger)
    {
        _catalog = catalog;
        _generationOrchestrator = generationOrchestrator;
        _logger = logger;
    }

    public async Task<ServiceResult<GenerationStartedResponse>> GenerateAsync(
        TemplateGenerationRequest request,
        Guid userId,
        string firebaseUid,
        string? signalRConnectionId,
        CancellationToken cancellationToken = default)
    {
        // 1. Format lookup
        var format = _catalog.GetFormat(request.FormatId);
        if (format == null)
        {
            _logger.LogWarning("Template format bulunamadı. | FormatId: {FormatId}", request.FormatId);
            return ServiceResult<GenerationStartedResponse>.Failure("Geçersiz format.", 400);
        }

        // 2. Prompt hazırla — kompozisyon suffix'i opsiyonel (transparent, kullanıcı kapatabilir)
        var prompt = request.Prompt?.Trim() ?? string.Empty;
        var finalPrompt = request.UsePromptSuffix
            ? $"{prompt}{format.PromptSuffix}"
            : prompt;

        _logger.LogInformation(
            "Template generation isteği. | UserId: {UserId} | Format: {FormatId} | Endpoint: {Endpoint} | Aspect: {Aspect}",
            userId, format.Id, format.ModelEndpoint, format.AspectRatioValue);

        // 3. Mevcut generation pipeline'ını reuse et. Kredi/tier/log/refund/SignalR
        //    aynen GenerationOrchestrator tarafından yürütülür. Templates yalnızca
        //    endpoint + aspect ratio'yu format'tan set eder (kişiselleştirme YOK).
        var startRequest = new StartGenerationRequest
        {
            ModelEndpoint = format.ModelEndpoint,
            Prompt = finalPrompt,
            AspectRatio = format.AspectRatioValue,
            NumImages = 1
        };

        return await _generationOrchestrator.StartGenerationAsync(
            startRequest, userId, firebaseUid, signalRConnectionId, cancellationToken);
    }
}
