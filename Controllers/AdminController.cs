using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.Admin;
using SelfAI.Entities.Enums;
using SelfAI.Services.Generation.Orchestrators;
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
        private readonly IModelCatalogOrchestrator _modelCatalogOrchestrator;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            IModelCatalogOrchestrator modelCatalogOrchestrator,
            ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _modelCatalogOrchestrator = modelCatalogOrchestrator;
            _logger = logger;
        }

        // F.9b — Admin ekranlarında tekrar kullanılan sabit dropdown listeleri.
        private static readonly List<string> ModelCategories = new()
        {
            "text-to-image", "text-to-audio", "text-to-video",
            "image-to-image", "image-to-video"
        };

        private static readonly List<string> ModelTiers = new()
        {
            "Fast", "Standard", "Premium", "CharacterLora", "VideoFast", "VideoPremium"
        };

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

        // ── F.9b — Model Catalog yönetimi ─────────────────────────────────────

        // F.9b — Paginated + filtrelenebilir model listesi (tüm status'ler).
        [HttpGet]
        public async Task<IActionResult> Models(
            string? statusFilter = null,
            string? categoryFilter = null,
            string? searchTerm = null,
            int page = 1,
            CancellationToken ct = default)
        {
            var filter = new AdminModelListFilter
            {
                Status = ParseStatusFilter(statusFilter),
                Category = string.IsNullOrWhiteSpace(categoryFilter) || categoryFilter == "all"
                    ? null
                    : categoryFilter,
                SearchTerm = searchTerm,
                Page = page,
                PageSize = 20
            };

            var listResult = await _modelCatalogOrchestrator.GetAdminListAsync(filter, ct);

            var vm = new AdminModelsListViewModel
            {
                ModelList = listResult.IsSuccess ? listResult.Data : null,
                StatusFilter = statusFilter ?? "all",
                CategoryFilter = categoryFilter ?? "all",
                SearchTerm = searchTerm,
                Page = page,
                SuccessMessage = TempData["ModelsSuccess"] as string,
                ErrorMessage = TempData["ModelsError"] as string
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditModel(Guid modelId, CancellationToken ct)
        {
            var detailResult = await _modelCatalogOrchestrator.GetAdminDetailAsync(modelId, ct);
            if (!detailResult.IsSuccess || detailResult.Data == null)
            {
                TempData["ModelsError"] = detailResult.Message ?? "Model bulunamadı.";
                return RedirectToAction(nameof(Models));
            }

            var d = detailResult.Data;
            var vm = new AdminModelEditViewModel
            {
                Id = d.Id,
                EndpointId = d.EndpointId,
                DisplayName = d.DisplayName,
                Description = d.Description,
                Category = d.Category,
                Provider = d.Provider,
                Tier = d.Tier,
                CostUsd = d.CostUsd,
                IsRecommended = d.IsRecommended,
                Status = d.Status,
                ThumbnailUrl = d.ThumbnailUrl,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                AvailableCategories = ModelCategories,
                AvailableTiers = ModelTiers
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModel(AdminModelEditViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCategories = ModelCategories;
                model.AvailableTiers = ModelTiers;
                return View(model);
            }

            var update = new AdminModelUpdateDto
            {
                DisplayName = model.DisplayName,
                Description = model.Description,
                Category = model.Category,
                Provider = model.Provider,
                Tier = model.Tier,
                CostUsd = model.CostUsd,
                IsRecommended = model.IsRecommended
            };

            var result = await _modelCatalogOrchestrator.UpdateAdminAsync(model.Id, update, ct);

            if (!result.IsSuccess)
            {
                TempData["ModelsError"] = result.Message;
                return RedirectToAction(nameof(EditModel), new { modelId = model.Id });
            }

            TempData["ModelsSuccess"] = $"Model '{model.DisplayName}' güncellendi.";
            return RedirectToAction(nameof(Models));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetModelStatus(
            Guid modelId,
            CatalogStatus newStatus,
            string? returnFilter,
            CancellationToken ct)
        {
            var result = await _modelCatalogOrchestrator.SetStatusAsync(modelId, newStatus, ct);

            if (!result.IsSuccess)
                TempData["ModelsError"] = result.Message;
            else
                TempData["ModelsSuccess"] = $"Model status → {newStatus}";

            return RedirectToAction(nameof(Models), new { statusFilter = returnFilter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncModels(
            string category = "text-to-image",
            CancellationToken ct = default)
        {
            _logger.LogInformation("Admin fal.ai sync başlattı. | Category: {Category}", category);

            var result = await _modelCatalogOrchestrator.SyncFromFalAiAsync(category, ct);

            if (!result.IsSuccess)
                TempData["ModelsError"] = result.Message ?? "Sync başarısız.";
            else
                TempData["ModelsSuccess"] = $"Sync tamamlandı ({category}): {result.Message ?? result.Data + " model"}";

            return RedirectToAction(nameof(Models), new { categoryFilter = category });
        }

        /// <summary>
        /// F.9b — UI filtre token'ı → CatalogStatus. "disabled" token'ı Hidden'a map edilir
        /// (enum'da Disabled yok; Hidden = "admin gizledi").
        /// </summary>
        private static CatalogStatus? ParseStatusFilter(string? filter) => filter?.ToLowerInvariant() switch
        {
            "approved" => CatalogStatus.Approved,
            "pending" => CatalogStatus.Pending,
            "disabled" => CatalogStatus.Hidden,
            _ => null
        };
    }
}
