using System.ComponentModel.DataAnnotations;

namespace SelfAI.DTOs.Templates;

/// <summary>
/// Templates sekmesi üretim isteği (F.M.10a). SignalR connectionId body'de DEĞİL —
/// X-SignalR-ConnectionId header'ıyla gelir (mevcut Studio/Image pattern'iyle aynı).
/// </summary>
public class TemplateGenerationRequest
{
    /// <summary>Format identifier. Örn: "instagram-post"</summary>
    [Required(ErrorMessage = "Format seçilmeli.")]
    public string FormatId { get; set; } = string.Empty;

    /// <summary>Kullanıcının prompt'u. Kompozisyon suffix'i backend'de eklenir.</summary>
    [Required(ErrorMessage = "Prompt gerekli.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Prompt 3-1000 karakter olmalı.")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcı kompozisyon suffix'ini kapattıysa false. Default: true (suffix eklenir).
    /// </summary>
    public bool UsePromptSuffix { get; set; } = true;
}
