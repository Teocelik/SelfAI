using Microsoft.AspNetCore.Mvc;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRenderNetService _renderNetService;

        public AccountController(IRenderNetService renderNetService)
        {
            _renderNetService = renderNetService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> UploadAsset(Asset image)
        {
            if (image == null || image.File == null || image.File.Length == 0)
            {
                ModelState.AddModelError("File", "Lütfen bir dosya seçin.");
                return View("Index");
            }
            try
            {
                // RenderNet API'sine varlık yükleme işlemi
                var result = await _renderNetService.UploadAssetAsync(image);
                ViewBag.Result = result;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Yükleme sırasında bir hata oluştu: {ex.Message}");
            }
            return View("Index");
        }

    }
}
