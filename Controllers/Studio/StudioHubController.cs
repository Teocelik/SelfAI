using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SelfAI.Controllers.Studio;

/// <summary>
/// Studio ana hub — kullanıcı hangi içerik türü üretmek istediğini seçer.
/// Grid layout, 4 kart: Görsel (aktif), Templates (yakında), Music (yakında),
/// Video (yakında). F.M.Arch.1'de eklendi.
/// </summary>
[Authorize]
[Route("Studio")]
public class StudioHubController : Controller
{
    // Studio kabuğu anonim erişilebilir kalır (mevcut RenderNet davranışı — sıfır regression).
    [AllowAnonymous]
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/Studio/Hub.cshtml");
    }
}
