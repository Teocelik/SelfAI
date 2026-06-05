using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfAI.DTOs.AuthDtos;
using SelfAI.Services.Interfaces;
using System.Security.Claims;

namespace SelfAI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly IUserService _userService;
        private readonly ICreditService _creditService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IFirebaseAuthService firebaseAuthService,
            IUserService userService,
            ICreditService creditService,
            ILogger<AccountController> logger)
        {
            _firebaseAuthService = firebaseAuthService;
            _userService = userService;
            _creditService = creditService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Dedicated full-page login (Affogato-style). Anonim kullanıcının giriş noktası.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Zaten giriş yapmış kullanıcı doğrudan studio'ya yönlendirilir.
            if (User.Identity?.IsAuthenticated == true)
            {
                return Redirect(string.IsNullOrEmpty(returnUrl) ? "/RenderNet/Index" : returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Frontend'den gelen Firebase ID token'ını doğrular ve başarılıysa auth çerezi kurar.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
            {
                return BadRequest(new { success = false, message = "Token boş." });
            }

            var result = await _firebaseAuthService.VerifyTokenAsync(request.IdToken);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, new { success = false, message = result.Message });
            }

            var userInfo = result.Data;

            // Token doğrulandı — iç DB'de AppUser'ı getir veya oluştur (yeni kullanıcıya 5 free credit verilir).
            var userResult = await _userService.GetOrCreateUserAsync(userInfo);

            if (!userResult.IsSuccess)
            {
                return StatusCode(userResult.StatusCode, new { success = false, message = userResult.Message });
            }

            var appUser = userResult.Data;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userInfo.Uid),       // Firebase UID
                new Claim("AppUserId", appUser.Id.ToString()),            // İç DB Id
                new Claim(ClaimTypes.Email, userInfo.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, userInfo.Name ?? userInfo.Email ?? userInfo.Uid),
                new Claim("Picture", userInfo.Picture ?? string.Empty),
                new Claim("EmailVerified", userInfo.EmailVerified.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                    AllowRefresh = true
                }
            );

            _logger.LogInformation(
                "Kullanıcı giriş yaptı. | Uid: {Uid} | Email: {Email}",
                userInfo.Uid, userInfo.Email
            );

            return Ok(new { success = true, message = "Giriş başarılı." });
        }

        // Auth çerezini temizler ve ana sayfaya yönlendirir.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Kullanıcı çıkış yaptı. | Uid: {Uid}", uid ?? "anonim");

            return RedirectToAction("Index", "Home");
        }

        // Giriş yapmış kullanıcının güncel kredi bakiyesini döner (top bar göstergesi için).
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Balance()
        {
            var appUserIdStr = User.FindFirst("AppUserId")?.Value;
            if (!Guid.TryParse(appUserIdStr, out var appUserId))
            {
                return Unauthorized(new { success = false, message = "Kimlik doğrulanamadı." });
            }

            var balance = await _creditService.GetBalanceAsync(appUserId);
            return Ok(new { success = true, balance = balance });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult EmailLoginCallback()
        {
            // Bu view, Firebase email link akışını client-side tamamlayan
            // sayfayı render eder. firebase-auth-callback.js bu sayfada çalışır,
            // email link'ten kimlik doğruladıktan sonra token'ı VerifyToken'a
            // POST eder.
            return View();
        }
    }
}
