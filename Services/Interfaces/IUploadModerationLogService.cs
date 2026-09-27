using SelfAI.DTOs.Moderation;
using SelfAI.Entities.Enums;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Görsel yükleme moderasyon kararlarının audit kaydını yazar (F.8b Faz A).
///
/// Ayrı servis olmasının nedeni: moderasyon servisi DB bilmemeli (SRP), ancak kayıt
/// birden fazla akıştan (Face Lock upload + karakter eğitimi) yazılıyor — çağıranların
/// her birinde tekrarlanmasın diye (DRY).
/// </summary>
public interface IUploadModerationLogService
{
    /// <summary>
    /// Bir moderasyon kararını kalıcı kaydeder. KABUL EDİLEN ve REDDEDİLEN her yükleme
    /// için çağrılır. Hash ve dosya boyutu <paramref name="imageBytes"/>'tan türetilir;
    /// görselin kendisi saklanmaz.
    ///
    /// Kayıt yazımı başarısız olursa exception fırlatmaz — audit hatası kullanıcının
    /// yükleme akışını bozmamalıdır (hata log'lanır).
    /// </summary>
    Task LogAsync(
        Guid appUserId,
        byte[] imageBytes,
        ImageModerationResult result,
        ModerationSource source,
        CancellationToken cancellationToken = default);
}
