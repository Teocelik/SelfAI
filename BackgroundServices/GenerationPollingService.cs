using Microsoft.AspNetCore.SignalR;
using SelfAI.Hubs;
using SelfAI.Services.Interfaces;
using System.Collections.Concurrent;

namespace SelfAI.BackgroundServices
{
    public class GenerationPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<GenerationHub> _hubContext;
        private readonly ILogger<GenerationPollingService> _logger;

        // ═══ AKTİF İŞLER ═══
        // Polling devam eden işler
        private readonly ConcurrentDictionary<string, PollingJob> _activeJobs = new();

        // ═══ 🆕 TAMAMLANAN AMA TESLİM EDİLEMEYEN SONUÇLAR ═══
        // Kullanıcı çevrimdışıyken tamamlanan işler burada bekler
        private readonly ConcurrentDictionary<string, List<CompletedResult>> _pendingResults = new();

        // ═══ 🆕 CLIENT → CONNECTION MAPPING ═══
        // Hangi clientId'nin hangi connectionId ile bağlı olduğunu tutar
        // Kullanıcı geri geldiğinde connectionId değişir, bunu güncellememiz lazım
        private readonly ConcurrentDictionary<string, string> _clientConnections = new();

        private const int POLLING_INTERVAL_MS = 3000;
        private const int MAX_ATTEMPTS = 60;
        // 🆕 Tamamlanan sonuçları ne kadar süre saklayalım (24 saat)
        private const int RESULT_RETENTION_HOURS = 24;

        public GenerationPollingService(
            IServiceProvider serviceProvider,
            IHubContext<GenerationHub> hubContext,
            ILogger<GenerationPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Yeni bir polling görevi ekle
        /// </summary>
        public void AddJob(string generationId, string clientId, string connectionId)
        {
            var job = new PollingJob
            {
                GenerationId = generationId,
                ClientId = clientId,           // 🆕 Kullanıcı kimliği (kalıcı)
                ConnectionId = connectionId,    // SignalR bağlantısı (değişebilir)
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            _activeJobs.TryAdd(generationId, job);

            // Client → Connection mapping'i güncelle
            _clientConnections.AddOrUpdate(clientId, connectionId, (_, __) => connectionId);

            _logger.LogInformation(
                "Polling job eklendi. | GenerationId: {GenId} | ClientId: {ClientId} | ConnectionId: {ConnId}",
                generationId, clientId, connectionId);
        }

        /// <summary>
        /// 🆕 Kullanıcı geri geldiğinde connectionId'yi güncelle
        /// ve bekleyen sonuçları gönder
        /// </summary>
        public async Task ClientReconnected(string clientId, string newConnectionId)
        {
            _logger.LogInformation(
                "Client yeniden bağlandı. | ClientId: {ClientId} | NewConnectionId: {ConnId}",
                clientId, newConnectionId);

            // 1. Connection mapping'i güncelle
            _clientConnections.AddOrUpdate(clientId, newConnectionId, (_, __) => newConnectionId);

            // 2. Aktif job'ların connectionId'sini güncelle
            foreach (var job in _activeJobs.Values.Where(j => j.ClientId == clientId))
            {
                job.ConnectionId = newConnectionId;
                _logger.LogDebug(
                    "Aktif job connectionId güncellendi. | GenerationId: {GenId}",
                    job.GenerationId);
            }

            // 3. Bekleyen sonuçları gönder
            await DeliverPendingResults(clientId, newConnectionId);
        }

        /// <summary>
        /// 🆕 Bekleyen sonuçları kullanıcıya gönder
        /// </summary>
        private async Task DeliverPendingResults(string clientId, string connectionId)
        {
            if (!_pendingResults.TryRemove(clientId, out var results))
                return; // Bekleyen sonuç yok

            _logger.LogInformation(
                "Bekleyen {Count} sonuç gönderiliyor. | ClientId: {ClientId}",
                results.Count, clientId);

            foreach (var result in results)
            {
                try
                {
                    string method = result.Type switch
                    {
                        ResultType.Completed => "GenerationCompleted",
                        ResultType.Failed => "GenerationFailed",
                        ResultType.Timeout => "GenerationTimeout",
                        _ => "GenerationCompleted"
                    };

                    await _hubContext.Clients.Client(connectionId)
                        .SendAsync(method, result.Data);

                    _logger.LogInformation(
                        "Bekleyen sonuç gönderildi. | GenerationId: {GenId} | Type: {Type}",
                        result.GenerationId, result.Type);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Bekleyen sonuç gönderilemedi, tekrar kaydediliyor. | GenerationId: {GenId}",
                        result.GenerationId);

                    // Gönderilemezse tekrar kaydet
                    SavePendingResult(clientId, result);
                }
            }
        }

        /// <summary>
        /// 🆕 Sonucu bekleyen sonuçlara kaydet (kullanıcı çevrimdışı)
        /// </summary>
        private void SavePendingResult(string clientId, CompletedResult result)
        {
            _pendingResults.AddOrUpdate(
                clientId,
                new List<CompletedResult> { result },           // İlk sonuç
                (_, existing) => { existing.Add(result); return existing; } // Listeye ekle
            );

            _logger.LogDebug(
                "Sonuç pending'e kaydedildi. | ClientId: {ClientId} | GenerationId: {GenId}",
                clientId, result.GenerationId);
        }

        /// <summary>
        /// Ana polling döngüsü
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GenerationPollingService başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_activeJobs.IsEmpty)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                foreach (var kvp in _activeJobs)
                {
                    var job = kvp.Value;
                    job.Attempts++;

                    // Timeout kontrolü
                    if (job.Attempts >= MAX_ATTEMPTS)
                    {
                        await HandleJobResult(job, ResultType.Timeout, new
                        {
                            generationId = job.GenerationId,
                            message = "Görsel oluşturma zaman aşımına uğradı."
                        });

                        _activeJobs.TryRemove(kvp.Key, out _);
                        continue;
                    }

                    await CheckGenerationStatus(job);
                }

                await Task.Delay(POLLING_INTERVAL_MS, stoppingToken);

                // 🆕 Periyodik olarak eski pending sonuçları temizle
                CleanupOldResults();
            }
        }

        /// <summary>
        /// Tek bir generation'ın durumunu kontrol et
        /// </summary>
        private async Task CheckGenerationStatus(PollingJob job)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var generationService = scope.ServiceProvider
                    .GetRequiredService<IRenderNetGenerationService>();

                var result = await generationService.GetGenerationAsync(job.GenerationId);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning(
                        "Polling: Durum alınamadı. | GenerationId: {GenId} | Attempt: {Attempt}",
                        job.GenerationId, job.Attempts);
                    return;
                }

                var media = result.Data.Data.Media;

                // ✅ TAMAMLANDI
                if (media.All(m => m.Status == "success"))
                {
                    var completedData = new
                    {
                        generationId = job.GenerationId,
                        media = media.Select(m => new
                        {
                            id = m.Id,
                            url = m.Url,
                            status = m.Status,
                            type = m.Type
                        }).ToList()
                    };

                    await HandleJobResult(job, ResultType.Completed, completedData);
                    _activeJobs.TryRemove(job.GenerationId, out _);
                    return;
                }

                // ❌ BAŞARISIZ
                if (media.Any(m => m.Status == "failed"))
                {
                    await HandleJobResult(job, ResultType.Failed, new
                    {
                        generationId = job.GenerationId,
                        message = "Görsel oluşturulurken bir hata oluştu."
                    });

                    _activeJobs.TryRemove(job.GenerationId, out _);
                    return;
                }

                // ⏳ DEVAM EDİYOR
                _logger.LogDebug(
                    "Polling: Hala işleniyor. | GenerationId: {GenId} | Attempt: {Attempt}/{Max}",
                    job.GenerationId, job.Attempts, MAX_ATTEMPTS);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Polling hatası! | GenerationId: {GenId}", job.GenerationId);
            }
        }

        /// <summary>
        /// 🆕 Job sonucunu işle: SignalR ile gönder veya pending'e kaydet
        /// </summary>
        private async Task HandleJobResult(PollingJob job, ResultType type, object data)
        {
            string method = type switch
            {
                ResultType.Completed => "GenerationCompleted",
                ResultType.Failed => "GenerationFailed",
                ResultType.Timeout => "GenerationTimeout",
                _ => "GenerationCompleted"
            };

            _logger.LogInformation(
                "Job sonuçlandı. | GenerationId: {GenId} | Type: {Type} | ClientId: {ClientId}",
                job.GenerationId, type, job.ClientId);

            // Güncel connectionId'yi al
            var currentConnectionId = _clientConnections.GetValueOrDefault(job.ClientId);

            // SignalR ile göndermeyi dene
            bool delivered = false;
            if (!string.IsNullOrEmpty(currentConnectionId))
            {
                try
                {
                    await _hubContext.Clients.Client(currentConnectionId)
                        .SendAsync(method, data);
                    delivered = true;

                    _logger.LogInformation(
                        "Sonuç SignalR ile gönderildi. | GenerationId: {GenId} | ConnectionId: {ConnId}",
                        job.GenerationId, currentConnectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "SignalR gönderimi başarısız, pending'e kaydedilecek. | GenerationId: {GenId}",
                        job.GenerationId);
                }
            }

            // 🆕 Gönderilemezse pending'e kaydet
            if (!delivered)
            {
                _logger.LogInformation(
                    "Kullanıcı çevrimdışı, sonuç pending'e kaydedildi. | GenerationId: {GenId} | ClientId: {ClientId}",
                    job.GenerationId, job.ClientId);

                SavePendingResult(job.ClientId, new CompletedResult
                {
                    GenerationId = job.GenerationId,
                    Type = type,
                    Data = data,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// 🆕 24 saatten eski pending sonuçları temizle
        /// </summary>
        private void CleanupOldResults()
        {
            var cutoff = DateTime.UtcNow.AddHours(-RESULT_RETENTION_HOURS);

            foreach (var kvp in _pendingResults)
            {
                kvp.Value.RemoveAll(r => r.CompletedAt < cutoff);

                if (kvp.Value.Count == 0)
                {
                    _pendingResults.TryRemove(kvp.Key, out _);
                }
            }
        }
    }

    // ═══ MODELLER ═══

    public class PollingJob
    {
        public string GenerationId { get; set; }
        public string ClientId { get; set; }       // 🆕 Kalıcı kullanıcı kimliği
        public string ConnectionId { get; set; }    // Değişebilir
        public int Attempts { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CompletedResult
    {
        public string GenerationId { get; set; }
        public ResultType Type { get; set; }
        public object Data { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public enum ResultType
    {
        Completed,
        Failed,
        Timeout
    }
}