using Microsoft.AspNetCore.Mvc;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
