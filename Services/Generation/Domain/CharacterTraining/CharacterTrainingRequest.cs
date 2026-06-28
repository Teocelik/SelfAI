namespace SelfAI.Services.Generation.Domain.CharacterTraining;

/// <summary>
/// fal.ai flux-lora-fast-training domain isteği.
/// NOT: fal.ai "images_data_url" alanı TEK bir ZIP arşivi URL'i bekler (görsel dizisi DEĞİL).
/// Orchestrator yüz görsellerini ZIP'leyip fal.ai storage'a yükler, URL'i buraya verir.
/// </summary>
public class CharacterTrainingRequest
{
    /// <summary>Yüz görsellerinin ZIP arşivinin fal.ai storage URL'i.</summary>
    public string ImagesDataUrl { get; set; } = string.Empty;

    /// <summary>Generation prompt'una eklenecek tetikleyici kelime.</summary>
    public string TriggerWord { get; set; } = string.Empty;

    /// <summary>Stylized karakter ise true (segmentasyon + auto-caption kapatılır).</summary>
    public bool IsStyle { get; set; } = false;

    /// <summary>Training adım sayısı.</summary>
    public int Steps { get; set; } = 1000;
}
