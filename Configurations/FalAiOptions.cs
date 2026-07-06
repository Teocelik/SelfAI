namespace SelfAI.Configurations;

/// <summary>
/// fal.ai provider yapılandırması. Gerçek API key User Secrets'ta
/// ("FalAiOptions:ApiKey"), diğer değerler appsettings.json'da.
/// </summary>
public class FalAiOptions
{
    /// <summary>
    /// Configuration section adı — Program.cs bind'inde kullanılır (magic string yerine).
    /// NOT: Değer "FalAiOptions" olarak korunur; User Secrets / appsettings anahtarları
    /// ("FalAiOptions:ApiKey") ile uyumluluk için değiştirilmez.
    /// </summary>
    public const string SectionName = "FalAiOptions";

    /// <summary>fal.ai API anahtarı. Authorization: Key &lt;ApiKey&gt; header'ında kullanılır.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Async queue endpoint base URL'i.</summary>
    public string BaseUrl { get; set; } = "https://queue.fal.run";

    /// <summary>Tek bir HTTP çağrısının (submit/status/result) timeout süresi.</summary>
    public int DefaultTimeoutSeconds { get; set; } = 60;

    /// <summary>Polling denemeleri arasındaki bekleme süresi (ms).</summary>
    public int PollingIntervalMs { get; set; } = 3000;

    /// <summary>Maksimum polling deneme sayısı. 60 × 3s = 3 dk max.</summary>
    public int MaxPollingAttempts { get; set; } = 60;
}
