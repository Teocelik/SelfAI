using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Music sekmesi (/Studio/Music) — F.M.10c'de doldurulacak.
/// Şimdilik "Yakında" placeholder view gösterir.
/// </summary>
[Authorize]
[Route("Studio/Music")]
public class MusicStudioController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Studio/Music.cshtml");
    }
}
