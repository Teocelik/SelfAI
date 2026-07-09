using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Video sekmesi (/Studio/Video) — F.M.9'da doldurulacak.
/// Şimdilik "Yakında" placeholder view gösterir.
/// </summary>
[Authorize]
[Route("Studio/Video")]
public class VideoStudioController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Studio/Video.cshtml");
    }
}
