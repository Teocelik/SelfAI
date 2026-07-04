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

    /// <summary>Genel amaçlı upload (F.M.7 sonrası kullanılabilir).</summary>
    /// <remarks>Değer 3 stabildir — F.M.UI.1a'da kaldırılan 2 numaralı değerin yerine geri kaymaz.</remarks>
    Generic = 3
}
