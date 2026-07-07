using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;

namespace SelfAI.Authorization
{
    /// <summary>
    /// "Admin" policy gereksinimi. İçerik taşımaz — sadece policy'yi işaretler.
    /// </summary>
    public class AdminRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// F.7.3 — Firebase/Cookie tabanlı admin yetkilendirmesi.
    ///
    /// Proje ASP.NET Core Identity kullanmadığı için (CLAUDE.md #14 — Firebase Auth aktif,
    /// Identity YASAK) klasik Role store yoktur. Bu handler, giriş yapmış kullanıcının
    /// cookie'sindeki email claim'ini AdminOptions.AllowedEmails listesine karşı kontrol eder.
    ///
    /// Login akışına, AppUser entity'sine veya DB'ye dokunmaz — tamamen mevcut cookie
    /// principal üzerinden çalışır.
    /// </summary>
    public class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
    {
        private readonly AdminOptions _options;
        private readonly ILogger<AdminAuthorizationHandler> _logger;

        public AdminAuthorizationHandler(
            IOptions<AdminOptions> options,
            ILogger<AdminAuthorizationHandler> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminRequirement requirement)
        {
            // Kimlik doğrulanmamışsa yetki verme (challenge → login redirect).
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Task.CompletedTask;
            }

            var isAllowed = _options.AllowedEmails
                .Any(allowed => string.Equals(allowed?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));

            if (isAllowed)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning(
                    "Admin erişimi reddedildi — email whitelist'te değil. | Email: {Email}",
                    email);
            }

            return Task.CompletedTask;
        }
    }
}
