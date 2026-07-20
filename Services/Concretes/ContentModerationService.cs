using System.Text.Json;
using System.Text.RegularExpressions;
using SelfAI.DTOs.Moderation;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes;

/// <summary>
/// Statik keyword blacklist tabanlı prompt moderasyonu (F.8). Singleton — blacklist JSON'u
/// startup'ta bir kez yüklenip memory'de tutulur. Eşleştirme word-boundary regex ile yapılır
/// (kısmi eşleşme değil → "children's book" gibi false pozitifler önlenir). Türkçe diacritics
/// normalize edilir ("çıplak" → "ciplak" gibi), böylece hem TR hem EN keyword'ler yakalanır.
///
/// Fail-open: JSON bulunamaz/parse edilemezse hiçbir prompt bloklanmaz (üretim akışı bozulmaz,
/// yalnızca 1. koruma katmanı devre dışı kalır — fal.ai safety checker + ToS hâlâ aktif).
/// </summary>
public class ContentModerationService : IContentModerationService
{
    private readonly ILogger<ContentModerationService> _logger;
    private readonly ModerationBlacklist _blacklist;

    public ContentModerationService(
        IWebHostEnvironment env,
        ILogger<ContentModerationService> logger)
    {
        _logger = logger;

        // JSON blacklist yükle (startup'ta bir kere, memory'de tut).
        var jsonPath = Path.Combine(env.ContentRootPath, "Data", "moderation-blacklist.json");

        if (!File.Exists(jsonPath))
        {
            _logger.LogError(
                "Moderation blacklist JSON bulunamadı: {Path}. Fail-open: hiçbir prompt bloklanmayacak!",
                jsonPath);
            _blacklist = new ModerationBlacklist { Categories = new() };
            return;
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            _blacklist = JsonSerializer.Deserialize<ModerationBlacklist>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new ModerationBlacklist { Categories = new() };

            var totalKeywords = _blacklist.Categories.Sum(c => c.Value.Keywords.Count);
            _logger.LogInformation(
                "Moderation blacklist yüklendi. | Version: {Version} | Kategoriler: {CategoryCount} | Toplam keyword: {KeywordCount}",
                _blacklist.Version, _blacklist.Categories.Count, totalKeywords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Moderation blacklist parse edilemedi. Fail-open: hiçbir prompt bloklanmayacak!");
            _blacklist = new ModerationBlacklist { Categories = new() };
        }
    }

    public ModerationResult CheckPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return new ModerationResult { IsBlocked = false };

        // Normalize: lowercase + Türkçe diacritics çevir (ç→c, ğ→g, ı→i, ö→o, ş→s, ü→u).
        var normalized = NormalizeForMatching(prompt);

        foreach (var (categoryKey, category) in _blacklist.Categories)
        {
            foreach (var keyword in category.Keywords)
            {
                var normalizedKeyword = NormalizeForMatching(keyword);

                // Word boundary eşleşmesi (kısmi eşleşme değil). Örn: "child" blacklist'te olsa
                // "children's book illustration" false pozitif OLMAMALI. \b kelime sınırı korur.
                var pattern = $@"\b{Regex.Escape(normalizedKeyword)}\b";

                if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                {
                    _logger.LogWarning(
                        "Prompt bloklandı. | Kategori: {Category} | Keyword: {Keyword} | Prompt preview: {Preview}",
                        categoryKey, keyword,
                        prompt.Length > 100 ? prompt.Substring(0, 100) + "..." : prompt);

                    return new ModerationResult
                    {
                        IsBlocked = true,
                        Category = categoryKey,
                        UserMessage = category.UserMessage,
                        MatchedKeyword = keyword  // Sadece log/audit için — kullanıcıya dönmez.
                    };
                }
            }
        }

        return new ModerationResult { IsBlocked = false };
    }

    private static string NormalizeForMatching(string input)
    {
        // Lowercase + Türkçe diacritics kaldır.
        var lower = input.ToLowerInvariant();

        var sb = new System.Text.StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            sb.Append(c switch
            {
                'ç' => 'c',
                'ğ' => 'g',
                'ı' => 'i',
                'ö' => 'o',
                'ş' => 's',
                'ü' => 'u',
                _ => c
            });
        }

        return sb.ToString();
    }
}

// ═══ JSON deserialization için internal modeller (F.8) ═══

internal class ModerationBlacklist
{
    public string Version { get; set; } = "";
    public string LastUpdated { get; set; } = "";
    public Dictionary<string, ModerationCategory> Categories { get; set; } = new();
}

internal class ModerationCategory
{
    public string DisplayName { get; set; } = "";
    public string UserMessage { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
}
