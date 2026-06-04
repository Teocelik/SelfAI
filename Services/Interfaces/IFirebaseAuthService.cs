using SelfAI.DTOs.AuthDtos;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IFirebaseAuthService
    {
        // Firebase ID token'ı server tarafında doğrular, geçerliyse kullanıcı bilgisini döner.
        Task<ServiceResult<FirebaseUserInfoDto>> VerifyTokenAsync(string idToken);
    }
}
