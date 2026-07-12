using System.ComponentModel.DataAnnotations;

namespace SelfAI.DTOs.Templates;

/// <summary>
/// Templates sekmesi üretim isteği (F.M.10a + F.M.10b). SignalR connectionId body'de DEĞİL —
/// X-SignalR-ConnectionId header'ıyla gelir (mevcut Studio/Image pattern'iyle aynı).
///
/// F.M.10b: FormatId artık [Required] DEĞİL — "FormatId VEYA PresetId" akışı controller'da
/// manuel doğrulanır. Karakter seçimi (opsiyonel) her iki akışta da geçerlidir; endpoint
/// override'ını controller/builder YAPMAZ, orchestrator CharacterId'den kendisi yapar.
/// </summary>
public class TemplateGenerationRequest
{
    /// <summary>Format identifier (F.M.10a akışı). Örn: "instagram-post". PresetId ile mutex.</summary>
    public string? FormatId { get; set; }

    /// <summary>
    /// Preset template identifier (F.M.10b akışı). Örn: "ig-story-new-product".
    /// Dolu ise preset + text overlay akışı; FormatId ile mutex (controller doğrular).
    /// </summary>
    public string? PresetId { get; set; }

    /// <summary>Kullanıcının prompt'u. Kompozisyon suffix'i backend'de eklenir.</summary>
    [Required(ErrorMessage = "Prompt gerekli.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Prompt 3-1000 karakter olmalı.")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcı kompozisyon suffix'ini kapattıysa false. Default: true (yalnızca format akışı;
    /// preset akışında suffix her zaman preset config'den eklenir).
    /// </summary>
    public bool UsePromptSuffix { get; set; } = true;

    /// <summary>
    /// Karakter ID (opsiyonel). Dolu ise orchestrator ModelEndpoint'i flux-lora'ya override
    /// eder, LoRA URL'i Character entity'sinden çeker, sahiplik + Ready doğrulamasını yapar.
    /// Controller/builder yalnızca bu ID'yi StartGenerationRequest'e taşır.
    /// </summary>
    public Guid? CharacterId { get; set; }

    /// <summary>
    /// Preset akışında text alanları. Key = PresetTextFieldDto.Id, Value = kullanıcı metni.
    /// Boş bırakılan alanlar overlay'de skip edilir.
    /// </summary>
    public Dictionary<string, string>? PresetTextValues { get; set; }
}
