using System.IO.Compression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SelfAI.BackgroundServices;
using SelfAI.Configurations;
using SelfAI.Data;
using SelfAI.Entities;
using SelfAI.Entities.Enums;
using SelfAI.Hubs;
using SelfAI.Models;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Domain.CharacterTraining;
using SelfAI.Services.Generation.Pricing;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Generation.Orchestrators;

/// <summary>
/// Karakter LoRA eğitimi koordinatörü (F.M.4).
///
/// Request scope'unda: validation + kredi pre-charge + Character kaydı.
/// Background task'ta (YENİ DI scope, IServiceScopeFactory): asset upload + ZIP +
/// training submit + polling kaydı. Hata olursa refund + Failed + SignalR.
///
/// NOT: Fire-and-forget task request scope'unu aşar. (1) Yüklenen dosyalar request
/// dispose olmadan BELLEĞE okunur (IFormFile stream'i sonradan erişilemez).
/// (2) Scoped servisler (AppDbContext, ICreditService, IFalAiStorageClient,
/// ICharacterTrainer) IServiceScopeFactory ile yeni scope'tan alınır.
/// IHubContext singleton olduğu için scope'tan çözülebilir.
/// </summary>
public class CharacterTrainingOrchestrator : ICharacterTrainingOrchestrator
{
    private const int MaxFaceImages = 10;
    private const int MinFaceImages = 1;
    private const long MaxImageSizeBytes = 15 * 1024 * 1024;  // 15MB
    private const int TrainingSteps = 1000;

    private readonly ICreditService _creditService;
    private readonly ICreditPricingService _pricingService;
    private readonly CreditPricingOptions _pricingOptions;
    private readonly AppDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LoraTrainingPollingService _pollingService;
    private readonly ILogger<CharacterTrainingOrchestrator> _logger;

    public CharacterTrainingOrchestrator(
        ICreditService creditService,
        ICreditPricingService pricingService,
        IOptions<CreditPricingOptions> pricingOptions,
        AppDbContext db,
        IServiceScopeFactory scopeFactory,
        LoraTrainingPollingService pollingService,
        ILogger<CharacterTrainingOrchestrator> logger)
    {
        _creditService = creditService;
        _pricingService = pricingService;
        _pricingOptions = pricingOptions.Value;
        _db = db;
        _scopeFactory = scopeFactory;
        _pollingService = pollingService;
        _logger = logger;
    }

    public async Task<ServiceResult<Guid>> StartTrainingAsync(
        CharacterCreateRequest request,
        Guid userId,
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 50)
            return ServiceResult<Guid>.Failure("İsim 1-50 karakter arası olmalı.", 400);

        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Length < 10)
            return ServiceResult<Guid>.Failure("Açıklama en az 10 karakter olmalı.", 400);

        if (request.CharacterType != "realistic" && request.CharacterType != "stylized")
            return ServiceResult<Guid>.Failure("Karakter tipi geçersiz.", 400);

        if (request.FaceImages == null || request.FaceImages.Count < MinFaceImages)
            return ServiceResult<Guid>.Failure($"En az {MinFaceImages} yüz görseli gerekli.", 400);

        if (request.FaceImages.Count > MaxFaceImages)
            return ServiceResult<Guid>.Failure($"En fazla {MaxFaceImages} yüz görseli yükleyebilirsin.", 400);

        foreach (var img in request.FaceImages)
        {
            if (img.Length == 0)
                return ServiceResult<Guid>.Failure("Boş görsel dosyası yüklenemez.", 400);
            if (img.Length > MaxImageSizeBytes)
                return ServiceResult<Guid>.Failure("Görsel boyutu en fazla 15MB olmalı.", 400);
            if (string.IsNullOrEmpty(img.ContentType) || !img.ContentType.StartsWith("image/"))
                return ServiceResult<Guid>.Failure("Sadece görsel dosyaları kabul edilir.", 400);
        }

        // 2. Dosyaları belleğe oku (background task request stream'ine erişemez)
        var uploadedImages = new List<UploadedImage>(request.FaceImages.Count);
        foreach (var img in request.FaceImages)
        {
            using var ms = new MemoryStream();
            await img.CopyToAsync(ms, cancellationToken);
            uploadedImages.Add(new UploadedImage(
                ms.ToArray(),
                string.IsNullOrWhiteSpace(img.FileName) ? "face.jpg" : Path.GetFileName(img.FileName),
                img.ContentType));
        }

        // 3. Kredi hesabı (tier-based markup — hardcode YOK, kural #4)
        var creditsRequired = _pricingService.CalculateUserCredits(
            _pricingOptions.CharacterTrainingCostUsd, ModelTier.CharacterLora);

        // 4. Kredi düşümü (pre-charge — başarısızlıkta refund). TryDeductAsync atomiktir.
        var deductionResult = await _creditService.TryDeductAsync(
            userId, creditsRequired, $"Character training: {request.Name}");
        if (!deductionResult.IsSuccess)
        {
            _logger.LogWarning(
                "Karakter eğitimi için kredi yetersiz. | UserId: {UserId} | Required: {Required}",
                userId, creditsRequired);
            return ServiceResult<Guid>.Failure(
                deductionResult.Message ?? "Kredi yetersiz.", deductionResult.StatusCode);
        }

        // 5. Character kaydı (Uploading status)
        var characterId = Guid.NewGuid();
        var triggerWord = GenerateTriggerWord();
        var isStyle = request.CharacterType == "stylized";

        var character = new Character
        {
            Id = characterId,
            UserId = userId,
            Name = request.Name,
            Prompt = request.Prompt,
            CharacterType = isStyle ? CharacterType.Stylized : CharacterType.Realistic,
            Status = CharacterStatus.Active,
            LoraTrainingStatus = LoraTrainingStatus.Uploading,
            TriggerWord = triggerWord,
            TrainingStartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.Characters.Add(character);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Karakter eğitimi başlatıldı. | CharId: {CharId} | UserId: {UserId} | TriggerWord: {TW} | Credits: {Credits}",
            characterId, userId, triggerWord, creditsRequired);

        // 6. Fire-and-forget — upload + training submit + polling kaydı
        _ = Task.Run(() => ExecuteTrainingFlowAsync(
            characterId, userId, firebaseUid, triggerWord, isStyle, uploadedImages, creditsRequired),
            CancellationToken.None);

        // 7. Hemen response — sonuç SignalR ile gelecek
        return ServiceResult<Guid>.Success(characterId,
            "Karakter eğitimi başlatıldı. Yaklaşık 5 dakika sürer.");
    }

    private async Task ExecuteTrainingFlowAsync(
        Guid characterId,
        Guid userId,
        string firebaseUid,
        string triggerWord,
        bool isStyle,
        List<UploadedImage> images,
        int creditsCharged)
    {
        using var scope = _scopeFactory.CreateScope();
        var storageClient = scope.ServiceProvider.GetRequiredService<IFalAiStorageClient>();
        var assetService = scope.ServiceProvider.GetRequiredService<IAssetService>();
        var trainer = scope.ServiceProvider.GetRequiredService<ICharacterTrainer>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var creditService = scope.ServiceProvider.GetRequiredService<ICreditService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GenerationHub>>();

        try
        {
            // F.M.7 — bireysel yüz görselleri kalıcı Asset olarak persist edilir
            // (reuse + thumbnail + FaceReferenceAssetIds). Background task'ta IFormFile yerine
            // bellekteki byte[]'ten stream açılır (IAssetService stream overload).
            var assetIds = new List<Guid>(images.Count);
            var faceUrls = new List<string>(images.Count);
            foreach (var img in images)
            {
                using var imgStream = new MemoryStream(img.Bytes);
                var uploadResult = await assetService.UploadAsync(
                    imgStream, img.FileName, img.ContentType, img.Bytes.LongLength,
                    userId, AssetPurpose.CharacterTraining);
                if (!uploadResult.IsSuccess)
                    throw new InvalidOperationException($"Asset upload başarısız: {uploadResult.Message}");
                assetIds.Add(uploadResult.Data!.Id);
                faceUrls.Add(uploadResult.Data!.Url);
            }

            // Training input ZIP arşivi (fal.ai images_data_url TEK zip URL bekler). ZIP kalıcı
            // Asset değil (ephemeral training artifact) — doğrudan storage client ile yüklenir.
            var zipBytes = BuildZipArchive(images);
            using var zipStream = new MemoryStream(zipBytes);
            var zipUrl = await storageClient.UploadAsync(
                zipStream, "training_images.zip", "application/zip");

            var character = await db.Characters.FindAsync(characterId);
            if (character == null)
            {
                _logger.LogWarning("Training flow: Character bulunamadı, iptal. | CharId: {CharId}", characterId);
                return;
            }

            character.FaceReferenceAssetIds = assetIds;
            character.ThumbnailUrl = faceUrls.FirstOrDefault();
            character.LoraTrainingStatus = LoraTrainingStatus.Training;
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Asset upload tamamlandı. | CharId: {CharId} | FaceCount: {Count} | AssetIds: {AssetIds} | ZipUrl: {ZipUrl}",
                characterId, faceUrls.Count, string.Join(",", assetIds), zipUrl);

            // Training submit
            var submitResult = await trainer.SubmitTrainingAsync(new CharacterTrainingRequest
            {
                ImagesDataUrl = zipUrl,
                TriggerWord = triggerWord,
                IsStyle = isStyle,
                Steps = TrainingSteps
            });

            character.LoraTrainingJobId = submitResult.RequestId;
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Training submit edildi. | CharId: {CharId} | JobId: {JobId}",
                characterId, submitResult.RequestId);

            // Polling service'e ekle — SignalR routing için firebaseUid + refund için credits taşınır.
            _pollingService.RegisterTrainingJob(characterId, userId, firebaseUid, submitResult.RequestId, creditsCharged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Karakter eğitimi akış hatası. | CharId: {CharId} | UserId: {UserId}",
                characterId, userId);

            // Refund (pre-charge geri ver)
            await creditService.RefundAsync(userId, creditsCharged, $"Character training failed: {characterId}");

            // Failed işaretle
            var character = await db.Characters.FindAsync(characterId);
            if (character != null)
            {
                character.LoraTrainingStatus = LoraTrainingStatus.Failed;
                character.TrainingFailureReason = "Eğitim başlatılırken bir hata oluştu.";
                await db.SaveChangesAsync();
            }

            // SignalR — Failed bildirimi (mevcut F.M.3 kalıbı: Clients.User(firebaseUid))
            await hubContext.Clients.User(firebaseUid).SendAsync("CharacterTrainingUpdate", new
            {
                characterId,
                status = "Failed",
                name = character?.Name,
                reason = "Eğitim başlatılırken bir hata oluştu. Krediniz iade edildi."
            });
        }
    }

    /// <summary>Yüz görsellerini tek bir ZIP arşivine paketler (in-memory).</summary>
    private static byte[] BuildZipArchive(List<UploadedImage> images)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (int i = 0; i < images.Count; i++)
            {
                var img = images[i];
                // Çakışmayı önlemek için index prefix'li benzersiz isim.
                var ext = Path.GetExtension(img.FileName);
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var entryName = $"image_{i + 1:D2}{ext}";

                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(img.Bytes, 0, img.Bytes.Length);
            }
        }
        return ms.ToArray();
    }

    /// <summary>Benzersiz trigger word üretir (örn. "SLF_X3K9MZ").</summary>
    private static string GenerateTriggerWord()
    {
        var guid = Guid.NewGuid().ToString("N").ToUpperInvariant();
        return $"SLF_{guid[..6]}";
    }

    /// <summary>Background task'a taşınan bellek-içi görsel kopyası.</summary>
    private sealed record UploadedImage(byte[] Bytes, string FileName, string ContentType);
}
