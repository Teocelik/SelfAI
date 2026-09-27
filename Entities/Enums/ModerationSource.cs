namespace SelfAI.Entities.Enums;

/// <summary>
/// Moderasyondan geçen yüklemenin hangi akıştan geldiği (F.8b Faz A).
/// DB'ye string olarak yazılır.
/// </summary>
public enum ModerationSource
{
    /// <summary>Studio Face Lock / generic tek dosya upload (/Assets/Upload).</summary>
    FaceLock = 0,

    /// <summary>Karakter LoRA eğitimi yüz görselleri (/Characters/Create).</summary>
    CharacterTraining = 1
}
