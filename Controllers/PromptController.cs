using Microsoft.AspNetCore.Mvc;
using SelfAI.Services.Interfaces;

namespace SelfAI.Controllers
{
    public class PromptController : Controller
    {
        private readonly IPromptService _promptService;
        private readonly ILogger<PromptController> _logger;

        public PromptController(IPromptService promptService, ILogger<PromptController> logger)
        {
            _promptService = promptService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var result = _promptService.GetAll();

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Prompt listesi alınamadı. | StatusCode: {StatusCode} | Message: {Message}",
                    result.StatusCode, result.Message);

                return StatusCode(result.StatusCode, new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }
    }
}
