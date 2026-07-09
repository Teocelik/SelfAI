using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SelfAI.Controllers
{
    /// <summary>
    /// F.M.Arch.1 sonrası bu controller yalnızca backward-compat için tutulur.
    /// /RenderNet ve /RenderNet/Index URL'leri /Studio/Image'a redirect eder —
    /// eski bookmark'lar bozulmasın diye. Görsel üretim iş mantığı artık
    /// Controllers/Studio/ImageStudioController'da yaşıyor. İleride (F.M.9 / launch)
    /// tamamen kaldırılabilir.
    /// </summary>
    [Authorize]
    public class RenderNetController : Controller
    {
        // Anonim erişilebilir: kullanıcı /RenderNet bookmark'ından önce login'e
        // takılmadan doğrudan /Studio/Image'a gitsin (orada Index [AllowAnonymous]).
        [AllowAnonymous]
        [HttpGet]
        [Route("RenderNet")]
        [Route("RenderNet/Index")]
        public IActionResult Index()
        {
            return Redirect("/Studio/Image");
        }
    }
}
