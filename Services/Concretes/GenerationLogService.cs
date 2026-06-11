using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.Entities;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class GenerationLogService : IGenerationLogService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<GenerationLogService> _logger;

        public GenerationLogService(AppDbContext db, ILogger<GenerationLogService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ServiceResult<Guid>> CreateAsync(Guid userId, string renderNetGenerationId, int cost, string promptSnapshot)
        {
            var gen = new Generation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RenderNetGenerationId = renderNetGenerationId,
                Cost = cost,
                Status = GenerationStatus.Pending,
                PromptSnapshot = promptSnapshot?.Length > 2000 ? promptSnapshot.Substring(0, 2000) : promptSnapshot,
                CreatedAt = DateTime.UtcNow
            };

            _db.Generations.Add(gen);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Generation kaydı oluşturuldu. | GenId: {GenId} | RenderNetId: {RenderNetId} | UserId: {Uid}",
                gen.Id, renderNetGenerationId, userId);

            return ServiceResult<Guid>.Success(gen.Id, "Generation kaydedildi.");
        }

        public async Task<ServiceResult<int>> UpdateStatusAsync(string renderNetGenerationId, GenerationStatus status)
        {
            var rowsAffected = await _db.Generations
                .Where(g => g.RenderNetGenerationId == renderNetGenerationId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(g => g.Status, status)
                    .SetProperty(g => g.CompletedAt, DateTime.UtcNow));

            return ServiceResult<int>.Success(rowsAffected, $"Status güncellendi: {status}");
        }

        public async Task<ServiceResult<int>> SaveMediaItemsAsync(
            string renderNetGenerationId,
            IEnumerable<string> urls,
            string mediaType = "image")
        {
            if (string.IsNullOrWhiteSpace(renderNetGenerationId))
            {
                return ServiceResult<int>.Failure("Generation ID geçersiz.", 400);
            }

            if (urls == null || !urls.Any())
            {
                return ServiceResult<int>.Failure("URL listesi boş.", 400);
            }

            // Generation kaydını bul
            var generation = await _db.Generations
                .FirstOrDefaultAsync(g => g.RenderNetGenerationId == renderNetGenerationId);

            if (generation == null)
            {
                _logger.LogWarning(
                    "Media kaydedilemedi — Generation bulunamadı. | RenderNetId: {RenderNetId}",
                    renderNetGenerationId);
                return ServiceResult<int>.Failure("Generation bulunamadı.", 404);
            }

            // Idempotency: aynı generation için zaten media kaydı varsa tekrar yazma.
            // D.2.5'te HandleJobResult ve DeliverPendingResults iki ayrı noktadan
            // tetiklenebilir (reconnect/race), duplicate kayıt olmamalı.
            var existingCount = await _db.GenerationMediaItems
                .CountAsync(m => m.GenerationId == generation.Id);

            if (existingCount > 0)
            {
                _logger.LogInformation(
                    "Media zaten kaydedilmiş, atlanıyor. | GenId: {GenId} | ExistingCount: {Count}",
                    generation.Id, existingCount);
                return ServiceResult<int>.Success(existingCount, "Media zaten mevcut.");
            }

            // Media item'ları sırayla kaydet (Order ile multi-model sıralaması korunur)
            var urlList = urls.ToList();
            var mediaItems = new List<GenerationMedia>();

            for (int i = 0; i < urlList.Count; i++)
            {
                mediaItems.Add(new GenerationMedia
                {
                    Id = Guid.NewGuid(),
                    GenerationId = generation.Id,
                    Url = urlList[i],
                    MediaType = mediaType,
                    Order = i,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.GenerationMediaItems.AddRange(mediaItems);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Media kayıtları oluşturuldu. | GenId: {GenId} | Count: {Count}",
                generation.Id, mediaItems.Count);

            return ServiceResult<int>.Success(mediaItems.Count, $"{mediaItems.Count} media kaydedildi.");
        }

        public async Task<ServiceResult<(IReadOnlyList<Generation> Items, int TotalCount)>> GetUserGenerationsAsync(
            Guid userId, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 12;

            var query = _db.Generations
                .Where(g => g.UserId == userId)
                .Include(g => g.MediaItems.OrderBy(m => m.Order))
                .OrderByDescending(g => g.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ServiceResult<(IReadOnlyList<Generation>, int)>.Success(
                (items.AsReadOnly(), totalCount),
                $"{items.Count} kayıt getirildi.");
        }
    }
}
