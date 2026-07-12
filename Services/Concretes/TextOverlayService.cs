using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SelfAI.DTOs.Templates;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes;

/// <summary>
/// ImageSharp tabanlı server-side text overlay render (F.M.10b). Singleton — font
/// koleksiyonu startup'ta bir kez yüklenir (thread-safe read-only kullanım).
///
/// NOT: Fonts 1.0.1'de <c>RichTextOptions</c> YOK (Fonts 2.0'da geldi); layout için
/// <see cref="TextOptions"/> kullanılır. Origin'e <see cref="PointF"/> atanır (ImageSharp
/// PointF → Vector2 implicit dönüşümü mevcut).
/// </summary>
public class TextOverlayService : ITextOverlayService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<TextOverlayService> _logger;
    private readonly FontCollection _fontCollection;

    public TextOverlayService(
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env,
        ILogger<TextOverlayService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _env = env;
        _logger = logger;

        // Font'lar startup'ta yüklenir (singleton). Dosya yoksa servis çalışır ama
        // overlay skip'lenir (WARNING log) — generation akışı bloke olmaz.
        _fontCollection = new FontCollection();

        var fontsPath = Path.Combine(_env.WebRootPath, "fonts");
        if (!Directory.Exists(fontsPath))
        {
            _logger.LogError("wwwroot/fonts klasörü bulunamadı — text overlay çalışmaz. | Path: {Path}", fontsPath);
            return;
        }

        foreach (var fontFile in new[] { "Inter-Regular.ttf", "Inter-Bold.ttf", "Inter-SemiBold.ttf" })
        {
            var fontPath = Path.Combine(fontsPath, fontFile);
            if (File.Exists(fontPath))
            {
                _fontCollection.Add(fontPath);
            }
            else
            {
                _logger.LogWarning("Font bulunamadı: {Font} | Path: {Path}", fontFile, fontPath);
            }
        }
    }

    public async Task<Stream> RenderAsync(
        string sourceImageUrl,
        IReadOnlyList<TextOverlaySpec> textOverlays,
        CancellationToken cancellationToken = default)
    {
        // 1. Kaynak görseli indir
        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(sourceImageUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        // 2. Image yükle
        using var image = await Image.LoadAsync<Rgba32>(sourceStream, cancellationToken);

        _logger.LogInformation(
            "Text overlay render başladı. | Source: {W}x{H} | Overlays: {Count}",
            image.Width, image.Height, textOverlays.Count);

        // 3. Her text overlay için render
        foreach (var overlay in textOverlays)
        {
            if (string.IsNullOrWhiteSpace(overlay.Text)) continue;

            // Font family lookup — "Inter-Bold"/"Inter-SemiBold" → base family "Inter".
            var baseFamilyName = overlay.FontName
                .Replace("-Bold", "", StringComparison.OrdinalIgnoreCase)
                .Replace("-SemiBold", "", StringComparison.OrdinalIgnoreCase)
                .Replace("-Regular", "", StringComparison.OrdinalIgnoreCase);

            var fontFamily = _fontCollection.Families.FirstOrDefault(f =>
                f.Name.Equals(baseFamilyName, StringComparison.OrdinalIgnoreCase));

            if (fontFamily == default)
            {
                _logger.LogWarning("Font family bulunamadı: {Font}, ilk yüklü font kullanılıyor.", overlay.FontName);
                fontFamily = _fontCollection.Families.FirstOrDefault();
                if (fontFamily == default)
                {
                    _logger.LogError("Hiç font yüklenmemiş, overlay skip edildi.");
                    continue;
                }
            }

            // SemiBold standart FontStyle değil → Regular'a yaklaşılır (best-effort).
            var fontStyle = overlay.FontName.Contains("Bold", StringComparison.OrdinalIgnoreCase)
                            && !overlay.FontName.Contains("SemiBold", StringComparison.OrdinalIgnoreCase)
                ? FontStyle.Bold
                : FontStyle.Regular;

            var font = fontFamily.CreateFont(overlay.FontSize, fontStyle);

            if (!Color.TryParseHex(overlay.Color, out var textColor))
            {
                textColor = Color.White;
            }

            var horizontalAlignment = overlay.Alignment switch
            {
                "center" => HorizontalAlignment.Center,
                "right" => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left
            };

            // Gölge (drop shadow) — okunabilirlik için beyaz metinlerde önerilir.
            if (overlay.DropShadow)
            {
                var shadowOptions = new TextOptions(font)
                {
                    Origin = new PointF(overlay.X + 2, overlay.Y + 2),
                    WrappingLength = overlay.MaxWidth,
                    HorizontalAlignment = horizontalAlignment
                };
                image.Mutate(ctx => ctx.DrawText(shadowOptions, overlay.Text, Color.Black.WithAlpha(0.6f)));
            }

            // Ana metin
            var textOptions = new TextOptions(font)
            {
                Origin = new PointF(overlay.X, overlay.Y),
                WrappingLength = overlay.MaxWidth,
                HorizontalAlignment = horizontalAlignment
            };
            image.Mutate(ctx => ctx.DrawText(textOptions, overlay.Text, textColor));
        }

        // 4. PNG stream olarak dön (caller dispose eder)
        var outputStream = new MemoryStream();
        await image.SaveAsPngAsync(outputStream, cancellationToken);
        outputStream.Position = 0;

        _logger.LogInformation("Text overlay render başarılı. | Output: {Size} bytes", outputStream.Length);

        return outputStream;
    }
}
