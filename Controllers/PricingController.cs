using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    public class PricingController : Controller
    {
        private readonly IPackageService _packageService;
        private readonly ILogger<PricingController> _logger;

        public PricingController(IPackageService packageService, ILogger<PricingController> logger)
        {
            _packageService = packageService;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var packages = await _packageService.GetActivePackagesAsync();
            return View(packages);
        }
    }
}
