using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using SelfAI.ViewModels;

namespace SelfAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        //TEST F�ELD
        private readonly IRenderNetAssetService _renderNetService;

        public HomeController(ILogger<HomeController> logger, IRenderNetAssetService renderNetService)
        {
            _logger = logger;
            _renderNetService = renderNetService;
        }

        #region Home sayfas� i�lemleri
        //Home sayfas�n� a�ar!
        public IActionResult Index()
        {
            _renderNetService.GetUploadUrlAsync();
            return View();
            
        }

        //Home sayfas�ndaki formu g�nderir!
        [HttpPost]
        public IActionResult Index(UserEmailViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return View(model);
            }

            /*
             * Burada, modele girilen e - posta adresini ilk olarak Cloudflare API �zerinden bot olup olmad���n� kontrol edece�iz.

             * E�er bot de�ilse, e-posta adresini FireBase Auth API'yi �zerinden, kay�tl� de�ilse
             * kardedece�iz veya kay�tl� ise oturumu a�aca��z.(FiraBase Auth bunu otomatik yapar)
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
