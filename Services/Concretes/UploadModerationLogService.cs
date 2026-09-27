using System.Security.Cryptography;
using SelfAI.Data;
using SelfAI.DTOs.Moderation;
using SelfAI.Entities;
using SelfAI.Entities.Enums;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes;

/// <summary>
/// Moderasyon audit kayıtlarını yazar (F.8b Faz A). Tek sorumluluk: kayıt yazmak —
/// karar vermez, eşik bilmez.
/// </summary>
public class UploadModerationLogService : IUploadModerationLogService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UploadModerationLogService> _logger;

    public UploadModerationLogService(
        AppDbContext db,
        ILogger<UploadModerationLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid appUserId,
        byte[] imageBytes,
        ImageModerationResult result,
        ModerationSource source,
        CancellationToken cancellationToken = default)
    {
        // Yapılandırılmış log — audit satırı yazılamasa bile karar iz bırakır.
        _logger.LogInformation(
            "Görsel moderasyon kararı. | UserId: {UserId} | Karar: {Decision} | Skor: {Score} | Kaynak: {Source}",
            appUserId, result.Decision, result.Score, source);

        try
        {
            _db.UploadModerationLogs.Add(new UploadModerationLog
            {
                Id = Guid.NewGuid(),
                AppUserId = appUserId,
                ContentHashSha256 = ComputeSha256(imageBytes),
                FileSizeBytes = imageBytes.LongLength,
                NsfwScore = result.Score,
                Decision = result.Decision,
                Source = source,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit yazımı kullanıcının yükleme akışını bozmamalı — karar zaten uygulandı.
            _logger.LogError(ex,
                "Moderasyon audit kaydı yazılamadı. | UserId: {UserId} | Karar: {Decision} | Kaynak: {Source}",
                appUserId, result.Decision, source);
        }
    }

    private static string ComputeSha256(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();  // 64 karakter
    }
}
