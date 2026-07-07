using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.Services.Interfaces;
using SelfAI.ViewModels.Admin;

namespace SelfAI.Controllers
{
    /// <summary>
    /// F.7.3 — Mini-admin. Kullanıcı arama + kredi ekleme (SQL yazmadan).
    ///
    /// Yetki: "Admin" policy (AdminAuthorizationHandler cookie'deki email claim'ini
    /// AdminOptions.AllowedEmails'e karşı kontrol eder). ASP.NET Core Identity Role YOK.
    /// Sadece HTTP transport — iş mantığı IAdminService'te.
    /// </summary>
    [Authorize(Policy = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Users));

        [HttpGet]
        public async Task<IActionResult> Users(string? email = null, CancellationToken ct = default)
        {
            var vm = new AdminUsersViewModel { SearchEmail = email };

            // Redirect sonrası (kredi eklenince) email query string ile gelir — kartı yeniden göster.
            if (!string.IsNullOrWhiteSpace(email))
            {
                var result = await _adminService.FindUserByEmailAsync(email, ct);
                if (result.IsSuccess)
                    vm.FoundUser = result.Data;
                else
                    vm.NotFoundMessage = result.Message;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchUser(AdminUsersViewModel model, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(model.SearchEmail))
            {
                ModelState.AddModelError(nameof(model.SearchEmail), "Email gerekli.");
                return View(nameof(Users), model);
            }

            var result = await _adminService.FindUserByEmailAsync(model.SearchEmail, ct);

            if (!result.IsSuccess)
            {
                model.NotFoundMessage = result.Message;
                return View(nameof(Users), model);
            }

            model.FoundUser = result.Data;
            return View(nameof(Users), model);
        }

        [HttpGet]
        public async Task<IActionResult> AddCredit(Guid userId, CancellationToken ct)
        {
            var result = await _adminService.FindUserByIdAsync(userId, ct);
            if (!result.IsSuccess || result.Data == null)
                return NotFound();

            var vm = new AddCreditViewModel
            {
                TargetUserId = result.Data.Id,
                TargetEmail = result.Data.Email,
                CurrentBalance = result.Data.CurrentBalance
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCredit(AddCreditViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await RefreshTargetAsync(model, ct);
                return View(model);
            }

            if (!Guid.TryParse(User.FindFirst("AppUserId")?.Value, out var adminUserId))
                return Unauthorized();

            var result = await _adminService.AddCreditAsync(
                model.TargetUserId,
                model.Amount,
                model.Note,
                adminUserId,
                ct);

            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                await RefreshTargetAsync(model, ct);
                return View(model);
            }

            TempData["AdminSuccess"] = $"{result.Data!.Amount} kredi başarıyla eklendi. " +
                                       $"Yeni bakiye: {result.Data.NewBalance}";
            return RedirectToAction(nameof(Users), new { email = model.TargetEmail });
        }

        /// <summary>
        /// Form yeniden render edilirken hedef kullanıcı email + güncel bakiyeyi tazeler.
        /// </summary>
        private async Task RefreshTargetAsync(AddCreditViewModel model, CancellationToken ct)
        {
            var result = await _adminService.FindUserByIdAsync(model.TargetUserId, ct);
            if (result.IsSuccess && result.Data != null)
            {
                model.TargetEmail = result.Data.Email;
                model.CurrentBalance = result.Data.CurrentBalance;
            }
        }
    }
}
