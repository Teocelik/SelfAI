namespace SelfAI.Entities.Enums;

/// <summary>
/// Yüklenen asset'in kullanım amacı (F.M.7). Tek Asset entity, Purpose ile ayrılır.
/// DB'de int olarak saklanır — değerler stabil, yeniden numaralama YASAK.
/// </summary>
public enum AssetPurpose
{
    /// <summary>Character training input (yüz görselleri, 4-6 adet).</summary>
    CharacterTraining = 0,

    /// <summary>Face Lock reference (tek yüz görseli).</summary>
    FaceLock = 1,

    /// <summary>Pose Lock reference (tek poz görseli). F.M.6'da deferred.</summary>
    PoseLock = 2,

    /// <summary>Genel amaçlı upload (F.M.7 sonrası kullanılabilir).</summary>
    Generic = 3
}
