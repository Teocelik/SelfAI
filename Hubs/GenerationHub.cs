using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SelfAI.BackgroundServices;
using System.Security.Claims;

namespace SelfAI.Hubs
{
    [Authorize]
    public class GenerationHub : Hub
    {
        private readonly ILogger<GenerationHub> _logger;
        private readonly GenerationPollingService _pollingService;

        public GenerationHub(
            ILogger<GenerationHub> logger,
            GenerationPollingService pollingService)
        {
            _logger = logger;
            _pollingService = pollingService;
        }

        /// <summary>
        /// Kullanıcı bağlandığında otomatik olarak çalışır.
        /// Firebase UID üzerinden:
        /// 1. userId → connectionId mapping güncellenir
        /// 2. Bekleyen sonuçlar varsa ANINDA teslim edilir
        /// Frontend artık manuel "RegisterClient" çağrısı yapmaz.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning(
                    "SignalR: Kimliği doğrulanamayan bağlantı reddedildi. | ConnectionId: {ConnId}",
                    Context.ConnectionId);
                Context.Abort();
                return;
            }

            _logger.LogInformation(
                "SignalR: Bağlantı kuruldu. | UserId: {Uid} | ConnectionId: {ConnId}",
                userId, Context.ConnectionId);

            // PollingService'e bildir → connectionId güncelle + bekleyen sonuçları gönder
            await _pollingService.UserConnectedAsync(userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(
                "SignalR: Bağlantı koptu. | ConnectionId: {ConnId} | Reason: {Reason}",
                Context.ConnectionId,
                exception?.Message ?? "Normal disconnect");

            // 🆕 Bağlantı koptuğunda JOB DURDURULMAZ!
            // Polling devam eder, sonuç pending'e kaydedilir.

            await base.OnDisconnectedAsync(exception);
        }
    }
}
