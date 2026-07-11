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

        // F.9a — Ana sayfa artık Dashboard (önceden Users'a redirect ediyordu).
        [HttpGet]
        public Task<IActionResult> Index(CancellationToken ct = default) => Dashboard(ct);

        // F.9a — Dashboard: bugün/bu hafta özet metrikleri.
        [HttpGet]
        public async Task<IActionResult> Dashboard(CancellationToken ct = default)
        {
            var statsResult = await _adminService.GetDashboardStatsAsync(ct);

            if (!statsResult.IsSuccess)
                _logger.LogWarning("Dashboard metrikleri yüklenemedi. | Mesaj: {Message}", statsResult.Message);

            var vm = new AdminDashboardViewModel
            {
                Stats = statsResult.IsSuccess ? statsResult.Data : null
            };
            // Index action'ı bu method'a delege ettiği için view adı explicit verilir
            // (aksi halde çalışan action adına 'Index.cshtml' aranır).
            return View("Dashboard", vm);
        }

        // F.9a — Paginated kullanıcı listesi. Arama (email/isim) + tarih filtresi query string ile.
        // F.7.3 tekil arama bu listenin arama kutusuna entegre edildi.
        [HttpGet]
        public async Task<IActionResult> Users(
            string? searchTerm = null,
            string? dateFilter = null,
            int page = 1,
            CancellationToken ct = default)
        {
            DateTime? registeredAfter = dateFilter switch
            {
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                _ => null
            };

            var listResult = await _adminService.GetUsersPaginatedAsync(
                searchTerm, registeredAfter, page, 20, ct);

            var vm = new AdminUsersListViewModel
            {
                UserList = listResult.IsSuccess ? listResult.Data : null,
                SearchTerm = searchTerm,
                DateFilter = dateFilter ?? "all",
                Page = page
            };

            return View(vm);
        }

        // F.9a — Kullanıcı detay + işlem geçmişi (tek sayfa).
        [HttpGet]
        public async Task<IActionResult> UserDetail(
            Guid userId,
            int transactionPage = 1,
            CancellationToken ct = default)
        {
            var userResult = await _adminService.GetUserDetailAsync(userId, ct);
            if (!userResult.IsSuccess)
            {
                TempData["AdminError"] = userResult.Message;
                return RedirectToAction(nameof(Users));
            }

            var txResult = await _adminService.GetUserTransactionsAsync(userId, transactionPage, 20, ct);

            var vm = new AdminUserDetailViewModel
            {
                User = userResult.Data,
                Transactions = txResult.IsSuccess ? txResult.Data : null,
                TransactionPage = transactionPage
            };

            return View(vm);
        }

        // F.7.3 — Tekil email arama formu. F.9a'da liste araması ile birleştirildiği için
        // artık Users?searchTerm=... paginated listesine yönlendirir (davranış korunur).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SearchUser(AdminUsersViewModel model, CancellationToken ct)
        {
            return RedirectToAction(nameof(Users), new { searchTerm = model.SearchEmail });
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
            // F.9a — Users artık paginated liste; searchTerm ile hedef kullanıcıya filtrelenmiş dön.
            return RedirectToAction(nameof(Users), new { searchTerm = model.TargetEmail });
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
