namespace SelfAI.DTOs.Moderation;

/// <summary>
/// Prompt moderasyon kontrolünün sonucu (F.8). <see cref="IsBlocked"/> true ise
/// üretim engellenir; kullanıcıya <see cref="UserMessage"/> döner. <see cref="MatchedKeyword"/>
/// yalnızca log/audit içindir, kullanıcıya ASLA gösterilmez.
/// </summary>
public class ModerationResult
{
    public bool IsBlocked { get; set; }
    public string? Category { get; set; }
    public string? UserMessage { get; set; }
    public string? MatchedKeyword { get; set; }  // Log için, kullanıcıya gösterilmez
}
