using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.Moderation;
using SelfAI.Entities.Enums;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes;

/// <summary>
/// fal.ai NSFW dedektörü ile görsel moderasyonu (F.8b Faz A).
///
/// Scoped kayıtlıdır: <see cref="IFalAiClient"/> typed HttpClient (scoped) olduğu için
/// bu servis singleton OLAMAZ — captive dependency oluşurdu. Prompt moderasyonu
/// (<see cref="ContentModerationService"/>) singleton kalmaya devam eder, ona dokunulmaz.
///
/// Görsel storage'a yazılmadan önce çağrıldığı için henüz public URL'i yoktur;
/// dedektöre Base64 data URI olarak gönderilir (fal.ai file input'larında desteklenir).
///
/// Eşikler <see cref="ModerationOptions"/>'tan gelir — hardcode YOK.
/// </summary>
public class FalAiImageModerationService : IImageModerationService
{
    private const string HardRejectMessage = "Bu görsel içerik politikamıza uymuyor.";
    private const string SoftRejectMessage = "Bu görsel kabul edilmedi. Lütfen farklı bir görsel deneyin.";
    private const string DetectorFailureMessage = "Doğrulama yapılamadı, tekrar deneyin.";

    private readonly IFalAiClient _falAiClient;
    private readonly ModerationOptions _options;
    private readonly ILogger<FalAiImageModerationService> _logger;

    public FalAiImageModerationService(
        IFalAiClient falAiClient,
        IOptions<ModerationOptions> options,
        ILogger<FalAiImageModerationService> logger)
    {
        _falAiClient = falAiClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<ImageModerationResult>> ModerateAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return ServiceResult<ImageModerationResult>.Failure("Görsel içeriği boş.", 400);

        var mimeType = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
        var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";

        FalAiNsfwResponse response;
        try
        {
            response = await _falAiClient.SubmitAndWaitAsync<FalAiNsfwResponse>(
                _options.NsfwModelEndpoint,
                new { image_url = dataUri },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Fail-closed: doğrulanamayan görsel GEÇİRİLMEZ. Ham hata kullanıcıya gitmez.
            _logger.LogError(ex,
                "Görsel moderasyon çağrısı başarısız. | Endpoint: {Endpoint} | SizeBytes: {Size}",
                _options.NsfwModelEndpoint, imageBytes.LongLength);

            return ServiceResult<ImageModerationResult>.Failure(DetectorFailureMessage, 503);
        }

        if (response == null)
        {
            _logger.LogError(
                "Görsel moderasyon yanıtı boş döndü. | Endpoint: {Endpoint}",
                _options.NsfwModelEndpoint);

            return ServiceResult<ImageModerationResult>.Failure(DetectorFailureMessage, 503);
        }

        var score = response.NsfwProbability;
        var decision = ResolveDecision(score);

        return ServiceResult<ImageModerationResult>.Success(new ImageModerationResult
        {
            Score = score,
            Decision = decision,
            UserMessage = decision switch
            {
                ModerationDecision.HardRejected => HardRejectMessage,
                ModerationDecision.SoftRejected => SoftRejectMessage,
                _ => string.Empty
            }
        });
    }

    /// <summary>Eşik karşılaştırması tek yerde — config'den okunur, hardcode edilmez.</summary>
    private ModerationDecision ResolveDecision(decimal score)
    {
        if (score >= _options.HardThreshold)
            return ModerationDecision.HardRejected;

        if (score >= _options.SoftThreshold)
            return ModerationDecision.SoftRejected;

        return ModerationDecision.Accepted;
    }
}
