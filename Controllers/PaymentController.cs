using Microsoft.AspNetCore.Mvc;

namespace SelfAI.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
