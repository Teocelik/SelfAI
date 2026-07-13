using Microsoft.AspNetCore.Mvc;
using SelfAI.Services.Interfaces;

namespace SelfAI.ViewComponents;

/// <summary>
/// F.M.UI.2 — Kredi bakiyesi göstergesi (top bar pill). Tek kaynaktan tüm
/// _AppLayout / _AdminLayout sayfalarında render edilir. Değer server-side
/// çekilir → sayfa yüklenir yüklenmez doğru rakam görünür (JS'e bağımlı "—"
/// flash'ı ortadan kalkar). SignalR/refresh güncellemeleri #creditBalanceValue
/// id'si üzerinden devam eder (app.js / template-studio.js / preset-studio.js).
///
/// MVC katman notu: bu bir view-tarafı bileşen. İş mantığı YOK — yalnızca
/// ICreditService.GetBalanceAsync okur ve fail gracefully davranır.
/// </summary>
public class CreditBalanceViewComponent : ViewComponent
{
    private readonly ICreditService _creditService;
    private readonly ILogger<CreditBalanceViewComponent> _logger;

    public CreditBalanceViewComponent(
        ICreditService creditService,
        ILogger<CreditBalanceViewComponent> logger)
    {
        _creditService = creditService;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Anonim kullanıcı — pill render edilmez (landing/login akışları etkilenmez).
        if (HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            return View(new CreditBalanceModel { IsAuthenticated = false });
        }

        // Kimlik claim'den çözülür (proje genelinde IUserContextService yok;
        // AccountController.Balance de aynı "AppUserId" claim'ini kullanır).
        var appUserIdStr = HttpContext.User.FindFirst("AppUserId")?.Value;
        if (!Guid.TryParse(appUserIdStr, out var appUserId))
        {
            _logger.LogWarning("Kredi bakiyesi component: AppUserId claim çözümlenemedi.");
            // Kullanıcı authenticated ama bakiye çözülemedi → pill görünür, değer "—".
            return View(new CreditBalanceModel { IsAuthenticated = true, Balance = null });
        }

        try
        {
            var balance = await _creditService.GetBalanceAsync(appUserId);
            return View(new CreditBalanceModel { IsAuthenticated = true, Balance = balance });
        }
        catch (Exception ex)
        {
            // Fail gracefully — bakiye göstergesi kritik akış değil, sayfa çökmesin.
            _logger.LogWarning(ex,
                "Kredi bakiyesi çekilemedi (component). | AppUserId: {AppUserId}", appUserId);
            return View(new CreditBalanceModel { IsAuthenticated = true, Balance = null });
        }
    }
}

/// <summary>
/// CreditBalance view'ine taşınan veri. Balance null ise bakiye çekilemedi
/// demektir → view "—" gösterir (çizgi degrade, hata değil).
/// </summary>
public class CreditBalanceModel
{
    public bool IsAuthenticated { get; set; }
    public int? Balance { get; set; }
}
