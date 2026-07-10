using SelfAI.DTOs.Templates;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Sosyal medya post formatları kataloğu (F.M.10a). Statik — 6 sabit format.
/// F.9'da DB-driven CRUD'a taşınabilir; interface aynı kalır.
/// </summary>
public interface ITemplateCatalogService
{
    /// <summary>Tüm formatları döner (F.M.10a: 6 sabit format).</summary>
    IReadOnlyList<PostFormatDto> GetAllFormats();

    /// <summary>Format ID ile arama. Bulunamazsa null.</summary>
    PostFormatDto? GetFormat(string formatId);
}
