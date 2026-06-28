using SelfAI.Models;

namespace SelfAI.Services.Generation.Abstractions;

/// <summary>
/// Karakter LoRA eğitimi iş akışı koordinatörü (F.M.4).
/// Kredi düşme + asset upload + training submit + DB persist + polling kaydını yönetir.
/// </summary>
public interface ICharacterTrainingOrchestrator
{
    /// <summary>
    /// Yeni karakter eğitimi başlatır:
    /// 1. Validation + kredi düşür
    /// 2. Character kaydı (Uploading)
    /// 3. (background) yüz görsellerini ZIP'leyip fal.ai storage'a upload
    /// 4. fal.ai LoRA training submit → job_id
    /// 5. LoraTrainingPollingService'e job ekle
    /// Hemen characterId döner; sonuç SignalR ile gelir.
    /// </summary>
    Task<ServiceResult<Guid>> StartTrainingAsync(
        CharacterCreateRequest request,
        Guid userId,
        string firebaseUid,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Karakter oluşturma isteği (multipart). Controller [FromForm] ile bind eder.
/// </summary>
public class CharacterCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string CharacterType { get; set; } = "realistic";  // "realistic" / "stylized"
    public List<IFormFile> FaceImages { get; set; } = new();
}
