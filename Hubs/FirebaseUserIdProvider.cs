using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SelfAI.Hubs
{
    /// <summary>
    /// SignalR'ın Clients.User(userId) altyapısı için kullanıcı kimliğini belirler.
    /// User.Identity.Name yerine Firebase UID (NameIdentifier claim'i) userId olarak işaretlenir.
    /// Böylece bir kullanıcının tüm aktif bağlantılarına (multi-tab) aynı anda yayın yapılabilir.
    /// </summary>
    public class FirebaseUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
