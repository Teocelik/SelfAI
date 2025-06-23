using Microsoft.AspNetCore.Mvc;
using SelfAI.Models;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRenderNetAssetService _renderNetService;

        public AccountController(IRenderNetAssetService renderNetService)
        {
            _renderNetService = renderNetService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // RenderNet API'sine varlık yükleme işlemi için gerekli metot
        public async Task<IActionResult> UploadAsset(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("File", "Lütfen bir dosya seçin.");
                return View("Index");
            }
            try
            {
                // RenderNet API'sine varlık yükleme işlemi
                var result = await _renderNetService.UploadAssetAsync(imageFile);
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
