using SelfAI.DTOs.Templates;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Server-side text overlay render servisi (F.M.10b). Üretilen görselin üstüne
/// preset config'deki sabit koordinat/font/renk ile kullanıcı metnini işler.
/// ImageSharp tabanlı; R2 upload caller (controller) sorumluluğundadır.
/// </summary>
public interface ITextOverlayService
{
    /// <summary>
    /// Kaynak görseli indirir, text alanlarını üzerine render eder, PNG olarak Stream döner.
    /// Boş <see cref="TextOverlaySpec.Text"/> olan spec'ler skip edilir.
    /// </summary>
    Task<Stream> RenderAsync(
        string sourceImageUrl,
        IReadOnlyList<TextOverlaySpec> textOverlays,
        CancellationToken cancellationToken = default);
}
