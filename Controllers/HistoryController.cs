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
        public async Task<IActionResult> Index(string? type = "all", int page = 1)
        {
            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            if (!Guid.TryParse(appUserIdStr, out var appUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (page < 1) page = 1;
            if (type is not ("all" or "image" or "music")) type = "all";

            var mediaTypeFilter = type == "all" ? null : type;

            var result = await _generationLogService.GetUserGenerationsAsync(
                appUserId, mediaTypeFilter, page, PageSize);
            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "History yüklenemedi. | UserId: {Uid} | Type: {Type} | Reason: {Reason}",
                    appUserId, type, result.Message);
                return View(new HistoryViewModel { CurrentType = type });
            }

            var (items, totalCount, imageCount, musicCount) = result.Data;
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            var viewModel = new HistoryViewModel
            {
                Generations = items,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = PageSize,
                CurrentType = type,
                ImageCount = imageCount,
                MusicCount = musicCount
            };

            return View(viewModel);
        }
    }
}
