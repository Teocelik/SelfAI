using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
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

        #region Home sayfas� i�lemleri
        //Home sayfas�n� a�ar!
        public IActionResult Index()
        {
            
            return View();
            
        }

        public IActionResult UploadImageAsync()
        {
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

        // Global hata yakalama metodu
        public IActionResult Error()
        {
            // Hata bilgilerini alal�m
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();

            // Hata nesnesini alal�m
            var exception = context?.Error; // Null olma durumu i�in null kontrol� yap�yoruz(?)

            // Hata detaylar�n� loglayal�m
            _logger.LogError(exception, "Global hata yakaland�: {Message}", exception?.Message);

            //Kullan�c�ya g�sterilecek hata sayfas� modeli
            var errorViewModel = new ErrorViewModel
            {
                Title = "Bir hata olu�tu",
                Message = "�zg�n�z, i�leminizi tamamlayamad�k. L�tfen tekrar deneyiniz."
            };

            return View(errorViewModel);
        }
    }
}
