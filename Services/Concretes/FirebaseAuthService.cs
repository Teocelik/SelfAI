using FirebaseAdmin.Auth;
using SelfAI.DTOs.AuthDtos;
using SelfAI.Models;  // ServiceResult için
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly ILogger<FirebaseAuthService> _logger;

        public FirebaseAuthService(ILogger<FirebaseAuthService> logger)
        {
            _logger = logger;
        }

        public async Task<ServiceResult<FirebaseUserInfoDto>> VerifyTokenAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                _logger.LogWarning("Token doğrulama isteği boş token ile geldi.");
                return ServiceResult<FirebaseUserInfoDto>.Failure("Token boş.", 400);
            }

            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

                var userInfo = new FirebaseUserInfoDto
                {
                    Uid = decodedToken.Uid,
                    Email = decodedToken.Claims.TryGetValue("email", out var email) ? email?.ToString() : null,
                    Name = decodedToken.Claims.TryGetValue("name", out var name) ? name?.ToString() : null,
                    Picture = decodedToken.Claims.TryGetValue("picture", out var pic) ? pic?.ToString() : null,
                    EmailVerified = decodedToken.Claims.TryGetValue("email_verified", out var ev) && (bool)ev
                };

                _logger.LogInformation(
                    "Firebase token doğrulandı. | Uid: {Uid} | Email: {Email}",
                    userInfo.Uid, userInfo.Email
                );

                return ServiceResult<FirebaseUserInfoDto>.Success(userInfo, "Token doğrulandı");
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning(
                    "Firebase token doğrulama başarısız. | Hata: {Message}",
                    ex.Message
                );
                return ServiceResult<FirebaseUserInfoDto>.Failure("Geçersiz token.", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase token doğrulama sırasında beklenmeyen hata.");
                return ServiceResult<FirebaseUserInfoDto>.Failure("Doğrulama hatası.", 500);
            }
        }
    }
}
