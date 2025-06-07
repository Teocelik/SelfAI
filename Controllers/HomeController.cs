using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SelfAI.Models;
using SelfAI.ViewModels;

namespace SelfAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        #region Home sayfasý iþlemleri
        //Home sayfasýný açar!
        public IActionResult Index()
        {
            return View();
        }

        //Home sayfasýndaki formu gönderir!
        [HttpPost]
        public IActionResult Index(UserEmailViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            /*
             * Burada, modele girilen e - posta adresini ilk olarak Cloudflare API üzerinden bot olup olmadýðýný kontrol edeceðiz.

             * Eðer bot deðilse, e-posta adresini FireBase Auth API'yi üzerinden, kayýtlý deðilse
             * kardedeceðiz veya kayýtlý ise oturumu açacaðýz.(FiraBase Auth bunu otomatik yapar)
             */

            return View();
        }
        #endregion

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
