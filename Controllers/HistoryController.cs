using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.Services.Interfaces;
using SelfAI.ViewModels;

namespace SelfAI.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly IGenerationLogService _generationLogService;
        private readonly ILogger<HistoryController> _logger;

        private const int PageSize = 12;

        public HistoryController(
            IGenerationLogService generationLogService,
            ILogger<HistoryController> logger)
        {
            _generationLogService = generationLogService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            if (!Guid.TryParse(appUserIdStr, out var appUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (page < 1) page = 1;

            var result = await _generationLogService.GetUserGenerationsAsync(appUserId, page, PageSize);
            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "History yüklenemedi. | UserId: {Uid} | Reason: {Reason}",
                    appUserId, result.Message);
                return View(new HistoryViewModel());
            }

            var (items, totalCount) = result.Data;
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            var viewModel = new HistoryViewModel
            {
                Generations = items,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize
            };

            return View(viewModel);
        }
    }
}
