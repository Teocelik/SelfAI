using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Post Templates sekmesi (/Studio/Templates) — F.M.10a'da doldurulacak.
/// Şimdilik "Yakında" placeholder view gösterir.
/// </summary>
[Authorize]
[Route("Studio/Templates")]
public class TemplatesStudioController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Studio/Templates.cshtml");
    }
}
