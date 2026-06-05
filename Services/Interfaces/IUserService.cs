using SelfAI.DTOs.AuthDtos;
using SelfAI.Entities;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface IUserService
    {
        // Firebase login sonrası: kullanıcı varsa getir, yoksa oluştur + 5 free credit
        Task<ServiceResult<AppUser>> GetOrCreateUserAsync(FirebaseUserInfoDto firebaseUser);
    }
}
