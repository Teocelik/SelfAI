using SelfAI.DTOs.Moderation;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Yüklenen görselin içerik politikasını ihlal edip etmediğini değerlendirir (F.8b Faz A).
///
/// Tek sorumluluk: "bu görsel ihlal mi?" sorusunu cevaplamak. Storage bilmez, DB bilmez,
/// HttpContext bilmez, log kaydı yazmaz — karar veren taraf çağırandır.
///
/// Prompt moderasyonu AYRI bir sorumluluktur (<see cref="IContentModerationService"/>);
/// iki abstraction kasıtlı olarak birleştirilmemiştir.
/// </summary>
public interface IImageModerationService
{
    /// <summary>
    /// Görseli dedektöre gönderir ve eşiklere göre karar üretir.
    ///
    /// Fail-closed: dedektör çağrısı başarısız olursa (timeout, 5xx, parse hatası)
    /// <see cref="ServiceResult{T}.IsSuccess"/> false döner — çağıran yüklemeyi REDDETMELİDİR.
    /// Doğrulanamayan içeriği geçirmek (fail-open) güvenlik açığıdır.
    /// </summary>
    /// <param name="imageBytes">Görselin ham içeriği (storage'a yazılmadan önce).</param>
    /// <param name="contentType">MIME tipi — data URI oluşturmak için (örn. "image/jpeg").</param>
    Task<ServiceResult<ImageModerationResult>> ModerateAsync(
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default);
}
