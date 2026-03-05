using Microsoft.AspNetCore.SignalR;
using SelfAI.BackgroundServices;

namespace SelfAI.Hubs
{
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
        /// 🆕 Kullanıcı bağlanıp clientId ile kendini tanıtır
        /// 
        /// Frontend bu metodu çağırır:
        ///   connection.invoke('RegisterClient', 'usr_abc-123');
        /// 
        /// Bu sayede:
        /// 1. Yeni connectionId ile eşleştirilir
        /// 2. Bekleyen sonuçlar varsa ANINDA gönderilir
        /// </summary>
        public async Task RegisterClient(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogWarning("RegisterClient: clientId boş! | ConnectionId: {ConnId}",
                    Context.ConnectionId);
                return;
            }

            _logger.LogInformation(
                "Client kaydedildi. | ClientId: {ClientId} | ConnectionId: {ConnId}",
                clientId, Context.ConnectionId);

            // PollingService'e bildir → connectionId güncelle + bekleyen sonuçları gönder
            await _pollingService.ClientReconnected(clientId, Context.ConnectionId);
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation(
                "SignalR: Bağlantı kuruldu. | ConnectionId: {ConnId}",
                Context.ConnectionId);

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