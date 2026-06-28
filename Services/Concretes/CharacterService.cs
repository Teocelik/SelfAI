using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.DTOs.Characters;
using SelfAI.Entities;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    // Karakter listeleme + arşivleme servisi (F.M.4).
    // Affogato bağımlılığı kaldırıldı — sadece DB. Oluşturma artık
    // ICharacterTrainingOrchestrator'da (fal.ai LoRA training).
    public class CharacterService : ICharacterService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<CharacterService> _logger;

        public CharacterService(AppDbContext db, ILogger<CharacterService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Sadece kullanıcının kendi Active karakterleri (Migrated/Archived hariç).
        // F.M.4 sonrası "system characters" kavramı YOK — sistem listesi getirilmez.
        public async Task<ServiceResult<CharacterListResponse>> GetUserCharactersAsync(
            Guid userId, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 20;

            // Id projeksiyonu (Guid.ToString) SQL'e çevrilmesin diye önce entity alanları çekilir.
            var rows = await _db.Characters
                .Where(c => c.UserId == userId && c.Status == CharacterStatus.Active)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Prompt,
                    c.ThumbnailUrl,
                    c.CharacterType,
                    c.LoraTrainingStatus,
                    c.TrainingFailureReason,
                    c.CreatedAt
                })
                .ToListAsync();

            var items = rows
                .Select(c => new CharacterDto
                {
                    Id = c.Id.ToString(),
                    Name = c.Name,
                    Prompt = c.Prompt,
                    ThumbnailUrl = c.ThumbnailUrl ?? "",
                    CharacterType = c.CharacterType == CharacterType.Realistic ? "realistic" : "stylized",
                    IsSystemCharacter = false,
                    TrainingStatus = c.LoraTrainingStatus.ToString(),
                    FailureReason = c.TrainingFailureReason,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            _logger.LogInformation(
                "Karakter listesi getirildi. | UserId: {Uid} | Count: {Count}",
                userId, items.Count);

            return ServiceResult<CharacterListResponse>.Success(new CharacterListResponse
            {
                Items = items.AsReadOnly(),
                TotalCount = items.Count
            }, $"{items.Count} karakter.");
        }

        // DB-only arşivleme. fal.ai karakterlerinde Affogato çağrısı YOK.
        public async Task<ServiceResult<bool>> ArchiveCharacterAsync(Guid userId, Guid characterId)
        {
            var character = await _db.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId
                                      && c.UserId == userId
                                      && c.Status == CharacterStatus.Active);

            if (character == null)
            {
                _logger.LogWarning(
                    "Archive engellendi (yetki yok veya karakter yok). | UserId: {Uid} | CharId: {CharId}",
                    userId, characterId);
                return ServiceResult<bool>.Failure("Bu karakter arşivlenemez.", 403);
            }

            character.Status = CharacterStatus.Archived;
            character.ArchivedAt = DateTime.UtcNow;
            character.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Karakter arşivlendi. | UserId: {Uid} | CharId: {CharId}",
                userId, characterId);

            return ServiceResult<bool>.Success(true, "Karakter arşivlendi.");
        }
    }
}
