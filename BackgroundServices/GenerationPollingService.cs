using Microsoft.AspNetCore.SignalR;
using SelfAI.Hubs;
using System.Collections.Concurrent;

namespace SelfAI.BackgroundServices
{
    /// <summary>
    /// SignalR kullanıcı-bağlantı takibi (Firebase UID → connectionId eşlemesi).
    ///
    /// NOT (F.M.8): Eski Affogato queue-polling alt-sistemi tamamen kaldırıldı
    /// (AddJob, ExecuteAsync döngüsü, CheckGenerationStatus, HandleJobResult,
    /// _activeJobs, _pendingResults + iç tipler). fal.ai generation queue polling'i
    /// artık FalAiClient.SubmitAndWaitAsync içinde yapılır; GenerationOrchestrator bunu
    /// fire-and-forget background task ile await eder ve sonucu SignalR "GenerationUpdate"
    /// event'iyle kendi IHubContext'inden push eder. Bu yüzden servis artık BackgroundService
    /// değil — yalnızca bağlantı eşlemesi tutan singleton.
    ///
    /// _userConnections + _hubContext şu an yalnızca bağlantı takibi için tutuluyor
    /// (F.M.UI.1'de bağlantı-bazlı push yeniden gerektiğinde hazır). Push mantığı
    /// halihazırda GenerationOrchestrator'da olduğu için burada aktif okuyucusu yok.
    /// </summary>
    public class GenerationPollingService
    {
        private readonly IHubContext<GenerationHub> _hubContext;
        private readonly ILogger<GenerationPollingService> _logger;

        // ═══ USER → CONNECTION MAPPING ═══
        // Hangi userId'nin (Firebase UID) hangi connectionId ile bağlı olduğunu tutar.
        // Kullanıcı geri geldiğinde connectionId değişir, bunu güncelleriz.
        private readonly ConcurrentDictionary<string, string> _userConnections = new();

        public GenerationPollingService(
            IHubContext<GenerationHub> hubContext,
            ILogger<GenerationPollingService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcı bağlandığında (GenerationHub.OnConnectedAsync) çağrılır:
        /// userId → connectionId eşlemesini günceller.
        /// </summary>
        public Task UserConnectedAsync(string userId, string newConnectionId)
        {
            _userConnections.AddOrUpdate(userId, newConnectionId, (_, __) => newConnectionId);

            _logger.LogInformation(
                "Kullanıcı bağlandı. | UserId: {Uid} | NewConnectionId: {ConnId}",
                userId, newConnectionId);

            return Task.CompletedTask;
        }
    }
}
