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
    }
}
