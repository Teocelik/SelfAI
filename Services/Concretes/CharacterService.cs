using Microsoft.EntityFrameworkCore;
using SelfAI.Data;
using SelfAI.DTOs.Characters;
using SelfAI.Entities;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using SelfAI.Services.Generation.Providers.Legacy.Affogato;
using System.Text;

namespace SelfAI.Services.Concretes
{
    // Karakter yönetimi business servisi: validation + Affogato API çağrısı + DB persist (F.6.1).
    public class CharacterService : ICharacterService
    {
        private readonly AppDbContext _db;
        private readonly IRenderNetCharacterService _apiClient;
        private readonly ILogger<CharacterService> _logger;

        public CharacterService(
            AppDbContext db,
            IRenderNetCharacterService apiClient,
            ILogger<CharacterService> logger)
        {
            _db = db;
            _apiClient = apiClient;
            _logger = logger;
        }

        // F.6.5 — Hibrit liste: kullanıcının kendi karakterleri (DB) + Affogato'nun
        // default sistem karakterleri (API, system_character=true filter). Affogato
        // çağrısı patlarsa sessizce sadece kullanıcı karakterleri döner (defansif).
        public async Task<ServiceResult<CharacterListResponse>> GetUserCharactersAsync(
            Guid userId, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 20;

            // 1) Kullanıcı karakterleri — DB'den. Id projeksiyonu (Guid.ToString())
            //    SQL'e çevrilmesin diye önce entity alanları çekilir, map bellekte yapılır.
            var userRows = await _db.Characters
                .Where(c => c.UserId == userId && c.Status == CharacterStatus.Active)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Prompt,
                    c.ThumbnailUrl,
                    c.CharacterType,
                    c.CreatedAt
                })
                .ToListAsync();

            var userChars = userRows
                .Select(c => new CharacterDto
                {
                    Id = c.Id.ToString(),
                    Name = c.Name,
                    Prompt = c.Prompt,
                    ThumbnailUrl = c.ThumbnailUrl,
                    CharacterType = c.CharacterType == CharacterType.Realistic ? "realistic" : "stylized",
                    IsSystemCharacter = false,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            // 2) Sistem karakterleri — Affogato API. Hata olursa user chars'a devam edilir.
            var systemChars = new List<CharacterDto>();
            var apiResult = await _apiClient.GetCharactersAsync(page: 1, pageSize: 50);
            if (apiResult.IsSuccess && apiResult.Data != null)
            {
                systemChars = apiResult.Data
                    .Where(c => c.SystemCharacter)  // Privacy: sadece default sistem karakterleri
                    .Select(c => new CharacterDto
                    {
                        Id = c.Id,                          // Affogato'nun "chr_xxx" ID'si
                        Name = c.Name,
                        Prompt = c.Prompt,
                        ThumbnailUrl = c.InputImage ?? "",  // Liste yanıtında input_image; yoksa placeholder
                        CharacterType = c.CharacterType,
                        IsSystemCharacter = true,
                        CreatedAt = null
                    })
                    .ToList();
            }
            else
            {
                _logger.LogWarning(
                    "Affogato sistem karakterleri çekilemedi (sadece kullanıcı karakterleri dönüyor). | UserId: {Uid}",
                    userId);
            }

            // 3) Merge: önce kullanıcı karakterleri, sonra sistem karakterleri.
            var allChars = userChars.Concat(systemChars).ToList();

            _logger.LogInformation(
                "Karakter listesi getirildi. | UserId: {Uid} | User: {UserCount} | System: {SystemCount}",
                userId, userChars.Count, systemChars.Count);

            return ServiceResult<CharacterListResponse>.Success(new CharacterListResponse
            {
                Items = allChars.AsReadOnly(),
                TotalCount = allChars.Count
            }, $"{userChars.Count} kullanıcı + {systemChars.Count} sistem karakter.");
        }

        // Validation → Affogato'ya unique name ile create → başarılıysa DB'ye persist.
        public async Task<ServiceResult<CharacterDto>> CreateCharacterAsync(
            Guid userId, CharacterCreateRequest request)
        {
            // Business validation
            if (string.IsNullOrWhiteSpace(request.Name))
                return ServiceResult<CharacterDto>.Failure("Karakter ismi gerekli.", 400);
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return ServiceResult<CharacterDto>.Failure("Karakter açıklaması gerekli.", 400);
            if (string.IsNullOrWhiteSpace(request.AssetId))
                return ServiceResult<CharacterDto>.Failure("Yüz görseli gerekli.", 400);
            if (request.CharacterType != "realistic" && request.CharacterType != "stylized")
                return ServiceResult<CharacterDto>.Failure("Karakter tipi geçersiz.", 400);

            // Kendi DB'mizde isim çakışması var mı?
            var nameTaken = await _db.Characters
                .AnyAsync(c => c.UserId == userId
                           && c.Name == request.Name
                           && c.Status == CharacterStatus.Active);
            if (nameTaken)
                return ServiceResult<CharacterDto>.Failure("Bu isimde bir karakterin zaten var.", 409);

            // Affogato için unique name üret (tüm SelfAI kullanıcıları aynı API key paylaştığı için).
            var sanitizedName = SanitizeName(request.Name);
            var uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var affogatoName = $"{sanitizedName}-{uniqueSuffix}";

            // Affogato'ya gönder
            var apiResult = await _apiClient.CreateCharacterAsync(
                request.AssetId,
                affogatoName,
                request.Prompt,
                request.CharacterType);

            if (!apiResult.IsSuccess || apiResult.Data is null)
            {
                _logger.LogWarning(
                    "Affogato character create başarısız. | UserId: {Uid} | Name: {Name}",
                    userId, request.Name);

                return ServiceResult<CharacterDto>.Failure(
                    "Karakter oluşturulamadı. Lütfen tekrar dene.", 502);
            }

            // DB'ye kaydet
            var character = new Character
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AffogatoCharacterId = apiResult.Data.Id,
                AffogatoCharacterName = apiResult.Data.Name ?? affogatoName,
                Name = request.Name,
                Prompt = request.Prompt,
                CharacterType = request.CharacterType == "realistic"
                    ? CharacterType.Realistic
                    : CharacterType.Stylized,
                ThumbnailUrl = apiResult.Data.InputImage,
                Status = CharacterStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _db.Characters.Add(character);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Karakter oluşturuldu. | UserId: {Uid} | CharId: {CharId} | Name: {Name}",
                userId, character.Id, character.Name);

            return ServiceResult<CharacterDto>.Success(new CharacterDto
            {
                Id = character.Id.ToString(),
                Name = character.Name,
                Prompt = character.Prompt,
                ThumbnailUrl = character.ThumbnailUrl,
                CharacterType = request.CharacterType,
                IsSystemCharacter = false,
                CreatedAt = character.CreatedAt
            }, "Karakter başarıyla oluşturuldu.");
        }

        // DB'de arşivle; Affogato arşivi başarısız olsa bile graceful devam (log warning).
        public async Task<ServiceResult<bool>> ArchiveCharacterAsync(Guid userId, Guid characterId)
        {
            // F.6.5 — System karakter koruması: characterId Guid'dir, yalnızca DB'deki
            // kullanıcı karakterlerine eşleşebilir. Sistem karakterlerinin Guid'i yoktur
            // ("chr_xxx" string), bu yüzden DB lookup null döner → 403. Başkasının
            // karakteri veya gerçekten yok olan karakter de aynı yanıtı alır (privacy).
            var character = await _db.Characters
                .FirstOrDefaultAsync(c => c.Id == characterId
                                      && c.UserId == userId
                                      && c.Status == CharacterStatus.Active);

            if (character == null)
            {
                _logger.LogWarning(
                    "Archive engellendi (system karakter veya yetki yok). | UserId: {Uid} | CharId: {CharId}",
                    userId, characterId);
                return ServiceResult<bool>.Failure("Bu karakter arşivlenemez.", 403);
            }

            // Affogato'da da arşivle. Başarısızlıkta graceful — DB'de yine arşivle,
            // senkronizasyon ileride background job ile düzeltilebilir.
            var affogatoResult = await _apiClient.ArchiveCharacterAsync(character.AffogatoCharacterId);
            if (!affogatoResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Affogato archive başarısız (DB'de yine de arşivleniyor). | CharId: {CharId} | AffId: {AffId}",
                    characterId, character.AffogatoCharacterId);
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

        // Affogato name için sanitize: sadece harf/rakam/_/-, boşluk → _.
        private static string SanitizeName(string name)
        {
            var sanitized = new StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c)) sanitized.Append(c);
                else if (c == ' ') sanitized.Append('_');
                else if (c == '-' || c == '_') sanitized.Append(c);
            }
            var result = sanitized.ToString();
            return string.IsNullOrEmpty(result) ? "character" : result;
        }
    }
}
