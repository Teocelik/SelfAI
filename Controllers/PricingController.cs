using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.Data;
using SelfAI.DTOs.SubscriptionDtos;
using SelfAI.Services.Interfaces;
using System.Security.Claims;

namespace SelfAI.Controllers
{
    public class PricingController : Controller
    {
        private readonly IPackageService _packageService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IIyzicoService _iyzicoService;
        private readonly AppDbContext _db;
        private readonly ILogger<PricingController> _logger;

        public PricingController(
            IPackageService packageService,
            ISubscriptionService subscriptionService,
            IIyzicoService iyzicoService,
            AppDbContext db,
            ILogger<PricingController> logger)
        {
            _packageService = packageService;
            _subscriptionService = subscriptionService;
            _iyzicoService = iyzicoService;
            _db = db;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var packages = await _packageService.GetActivePackagesAsync();
            return View(packages);
        }

        // Frontend "Abone Ol" → Subscription + Payment kaydı oluştur, Iyzico checkout başlat,
        // checkout sayfası URL'ini döndür (frontend oraya yönlendirir).
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request)
        {
            if (request == null || request.PackageId <= 0)
            {
                return BadRequest(new { success = false, message = "Geçersiz paket." });
            }

            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            if (!Guid.TryParse(appUserIdStr, out var userId))
            {
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
            }

            // 1. Subscription + Payment kayıtlarını oluştur (Pending)
            var initResult = await _subscriptionService.InitiateSubscriptionAsync(userId, request.PackageId);
            if (!initResult.IsSuccess)
            {
                return StatusCode(initResult.StatusCode, new { success = false, message = initResult.Message });
            }

            var (subscription, payment) = initResult.Data;

            // 2. Iyzico checkout başlat
            var callbackUrl = Url.Action("Callback", "Pricing", null, Request.Scheme)!;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "user@example.com";
            var pkg = await _db.Packages.FindAsync(request.PackageId);
            var packageName = pkg?.Name ?? "Paket";

            var iyzResult = await _iyzicoService.InitiateCheckoutAsync(
                subscription.Id,
                payment.IyzicoConversationId,
                payment.Amount,
                packageName,
                userEmail,
                callbackUrl);

            if (!iyzResult.IsSuccess)
            {
                // Iyzico başlatma başarısız — kayıtları geri al
                await _subscriptionService.CancelPendingSubscriptionAsync(
                    payment.IyzicoConversationId,
                    $"Iyzico başlatma hatası: {iyzResult.Message}");
                return StatusCode(iyzResult.StatusCode, new { success = false, message = iyzResult.Message });
            }

            // Payment'a checkout token'ını kaydet (audit/eşleştirme için)
            payment.IyzicoToken = iyzResult.Data!.Token;
            payment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                paymentPageUrl = iyzResult.Data.PaymentPageUrl
            });
        }

        // Iyzico ödeme sonrası buraya POST eder (cookie göndermez → AllowAnonymous).
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [Route("Pricing/Callback")]
        public async Task<IActionResult> Callback([FromForm] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction("PaymentFailed", new { reason = "Token alınamadı." });
            }

            // 1. Iyzico'dan ödeme sonucunu çek
            var retrieveResult = await _iyzicoService.RetrievePaymentResultAsync(token);
            if (!retrieveResult.IsSuccess || retrieveResult.Data == null || !retrieveResult.Data.IsSuccess)
            {
                var reason = retrieveResult.Data?.ErrorMessage;
                if (string.IsNullOrWhiteSpace(reason)) reason = retrieveResult.Message;

                await _subscriptionService.CancelPendingSubscriptionAsync(
                    retrieveResult.Data?.ConversationId ?? "unknown",
                    $"Iyzico ödeme başarısız: {reason}");

                return RedirectToAction("PaymentFailed", new { reason });
            }

            // 2. Subscription'ı aktive et + cüzdan top-up
            var activationResult = await _subscriptionService.ActivateSubscriptionAsync(
                retrieveResult.Data.ConversationId,
                retrieveResult.Data.PaymentId);

            if (!activationResult.IsSuccess)
            {
                _logger.LogError(
                    "Iyzico ödemesi başarılı ama subscription aktive edilemedi! | ConvId: {ConvId} | PaymentId: {PaymentId}",
                    retrieveResult.Data.ConversationId, retrieveResult.Data.PaymentId);
                return RedirectToAction("PaymentFailed", new
                {
                    reason = "Ödeme alındı ama abonelik aktive edilemedi. Lütfen destek ile iletişime geçin."
                });
            }

            return RedirectToAction("PaymentSuccess", new
            {
                packageName = activationResult.Data!.Package?.Name
            });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult PaymentSuccess(string packageName)
        {
            ViewBag.PackageName = packageName;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult PaymentFailed(string reason)
        {
            ViewBag.Reason = reason;
            return View();
        }
    }
}
