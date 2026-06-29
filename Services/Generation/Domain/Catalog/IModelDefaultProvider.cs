namespace SelfAI.Services.Generation.Domain.Catalog;

/// <summary>
/// Bir modelin generic payload'una eklenecek model-spesifik default değerleri
/// sağlar (F.M.5). Örn. Recraft için style, Seedream için watermark.
/// NOT: F.M.UI.1'de OpenAPI schema parsing ile değiştirilebilir; şimdilik
/// hardcoded map yeterli.
/// </summary>
public interface IModelDefaultProvider
{
    /// <summary>
    /// Endpoint'e özel default input alanlarını döner. Bilinmeyen endpoint
    /// için boş dictionary döner (sadece common alanlar gönderilir).
    /// </summary>
    Dictionary<string, object> GetDefaultsFor(string endpointId);
}
