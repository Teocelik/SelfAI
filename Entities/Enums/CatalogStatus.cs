namespace SelfAI.Entities.Enums;

/// <summary>
/// Model catalog girişinin yaşam döngüsü durumu (F.M.5).
/// Sadece <see cref="Approved"/> modeller Studio'da kullanıcıya gösterilir.
/// </summary>
public enum CatalogStatus
{
    /// <summary>Admin onaylı, kullanıcıya görünür.</summary>
    Approved = 0,

    /// <summary>fal.ai'da yeni keşfedildi, admin onayı bekliyor.</summary>
    Pending = 1,

    /// <summary>Admin tarafından gizlendi.</summary>
    Hidden = 2,

    /// <summary>fal.ai'da deprecated olmuş.</summary>
    Deprecated = 3
}
