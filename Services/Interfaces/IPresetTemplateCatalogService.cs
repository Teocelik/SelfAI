using SelfAI.DTOs.Templates;

namespace SelfAI.Services.Interfaces;

/// <summary>
/// Hazır şablon kataloğu (F.M.10b). Statik — 5 sabit preset. F.9b'de admin CRUD gelir,
/// interface aynı kalır. F.M.10a <see cref="ITemplateCatalogService"/> ile paralel yaşar.
/// </summary>
public interface IPresetTemplateCatalogService
{
    /// <summary>Tüm preset'leri döner (F.M.10b: 5 sabit şablon).</summary>
    IReadOnlyList<PresetTemplateDto> GetAllPresets();

    /// <summary>Preset ID ile arama. Bulunamazsa null.</summary>
    PresetTemplateDto? GetPreset(string presetId);
}
