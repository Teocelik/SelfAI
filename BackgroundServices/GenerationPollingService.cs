using Microsoft.AspNetCore.SignalR;
using SelfAI.Entities;
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

        // ═══ 🆕 USER → CONNECTION MAPPING ═══
        // Hangi userId'nin (Firebase UID) hangi connectionId ile bağlı olduğunu tutar.
        // Kullanıcı geri geldiğinde connectionId değişir, bunu güncellememiz lazım.
        // Bildirim gönderimi Clients.User(userId) ile yapıldığı için bu map ağırlıklı
        // olarak "kullanıcı online mı?" kontrolü ve job connectionId takibi için kullanılır.
        private readonly ConcurrentDictionary<string, string> _userConnections = new();

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
        public void AddJob(string generationId, string userId, string connectionId)
        {
            var job = new PollingJob
            {
                GenerationId = generationId,
                UserId = userId,                // 🆕 Firebase UID (kalıcı kullanıcı kimliği)
                ConnectionId = connectionId,    // SignalR bağlantısı (değişebilir)
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            _activeJobs.TryAdd(generationId, job);

            // User → Connection mapping'i güncelle
            _userConnections.AddOrUpdate(userId, connectionId, (_, __) => connectionId);

            _logger.LogInformation(
                "Polling job eklendi. | GenerationId: {GenId} | UserId: {Uid} | ConnectionId: {ConnId}",
                generationId, userId, connectionId);
        }

        /// <summary>
        /// 🆕 Kullanıcı bağlandığında (Hub.OnConnectedAsync) otomatik çağrılır:
        /// connectionId'yi güncelle ve bekleyen sonuçları gönder.
        /// </summary>
        public async Task UserConnectedAsync(string userId, string newConnectionId)
        {
            _logger.LogInformation(
                "Kullanıcı bağlandı. | UserId: {Uid} | NewConnectionId: {ConnId}",
                userId, newConnectionId);

            // 1. Connection mapping'i güncelle
            _userConnections.AddOrUpdate(userId, newConnectionId, (_, __) => newConnectionId);

            // 2. Aktif job'ların connectionId'sini güncelle
            foreach (var job in _activeJobs.Values.Where(j => j.UserId == userId))
            {
                job.ConnectionId = newConnectionId;
                _logger.LogDebug(
                    "Aktif job connectionId güncellendi. | GenerationId: {GenId}",
                    job.GenerationId);
            }

            // 3. Bekleyen sonuçları gönder
            await DeliverPendingResults(userId, newConnectionId);
        }

        /// <summary>
        /// 🆕 Bekleyen sonuçları kullanıcıya gönder
        /// </summary>
        private async Task DeliverPendingResults(string userId, string connectionId)
        {
            if (!_pendingResults.TryRemove(userId, out var results))
                return; // Bekleyen sonuç yok

            _logger.LogInformation(
                "Bekleyen {Count} sonuç gönderiliyor. | UserId: {Uid}",
                results.Count, userId);

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

                    // Önce DB Generation status'unu güncelle, sonra teslim et.
                    // (Sıralama önemli: push başarısız olsa bile DB tutarlı kalır.)
                    await UpdateGenerationStatusAsync(result.GenerationId, result.Type);

                    // Sadece o anda bağlanan bağlantıya teslim et (anlık reconnect teslimatı)
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
                    SavePendingResult(userId, result);
                }
            }
        }

        /// <summary>
        /// 🆕 Sonucu bekleyen sonuçlara kaydet (kullanıcı çevrimdışı)
        /// </summary>
        private void SavePendingResult(string userId, CompletedResult result)
        {
            _pendingResults.AddOrUpdate(
                userId,
                new List<CompletedResult> { result },           // İlk sonuç
                (_, existing) => { existing.Add(result); return existing; } // Listeye ekle
            );

            _logger.LogDebug(
                "Sonuç pending'e kaydedildi. | UserId: {Uid} | GenerationId: {GenId}",
                userId, result.GenerationId);
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
                "Job sonuçlandı. | GenerationId: {GenId} | Type: {Type} | UserId: {Uid}",
                job.GenerationId, type, job.UserId);

            // Kullanıcı online mı? (mapping varsa online kabul edilir)
            var currentConnectionId = _userConnections.GetValueOrDefault(job.UserId);

            // SignalR ile göndermeyi dene.
            // Clients.User(userId): kullanıcının TÜM aktif bağlantılarına (multi-tab) yayar.
            bool delivered = false;
            if (!string.IsNullOrEmpty(currentConnectionId))
            {
                try
                {
                    // Önce DB Generation status'unu güncelle, sonra push et.
                    // (Sıralama önemli: push başarısız olsa bile DB tutarlı kalır.)
                    await UpdateGenerationStatusAsync(job.GenerationId, type);

                    await _hubContext.Clients.User(job.UserId)
                        .SendAsync(method, data);
                    delivered = true;

                    _logger.LogInformation(
                        "Sonuç SignalR ile gönderildi. | GenerationId: {GenId} | UserId: {Uid}",
                        job.GenerationId, job.UserId);
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
                    "Kullanıcı çevrimdışı, sonuç pending'e kaydedildi. | GenerationId: {GenId} | UserId: {Uid}",
                    job.GenerationId, job.UserId);

                SavePendingResult(job.UserId, new CompletedResult
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

        /// <summary>
        /// 🆕 Polling sonuçlandığında DB'deki Generation kaydının status'unu günceller.
        /// Singleton servis olduğu için scoped IGenerationLogService'i scope açarak çözer
        /// (mevcut CheckGenerationStatus'taki _serviceProvider.CreateScope() kalıbıyla aynı).
        /// DB güncellemesi başarısız olursa SignalR push akışını engellemez — sadece uyarı loglanır.
        /// </summary>
        private async Task UpdateGenerationStatusAsync(string renderNetGenerationId, ResultType type)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var logService = scope.ServiceProvider.GetRequiredService<IGenerationLogService>();

                var status = type switch
                {
                    ResultType.Completed => GenerationStatus.Completed,
                    ResultType.Failed => GenerationStatus.Failed,
                    ResultType.Timeout => GenerationStatus.Failed,
                    _ => GenerationStatus.Completed
                };

                await logService.UpdateStatusAsync(renderNetGenerationId, status);

                _logger.LogInformation(
                    "DB Generation status güncellendi. | GenerationId: {GenId} | Status: {Status}",
                    renderNetGenerationId, status);
            }
            catch (Exception ex)
            {
                // DB güncellemesi başarısız olursa SignalR push'u engelleme — sadece uyarı logla.
                _logger.LogWarning(ex,
                    "DB Generation status güncellenemedi (SignalR push devam ediyor). | GenerationId: {GenId}",
                    renderNetGenerationId);
            }
        }
    }

    // ═══ MODELLER ═══

    public class PollingJob
    {
        public string GenerationId { get; set; }
        public string UserId { get; set; }          // 🆕 Firebase UID (kalıcı kullanıcı kimliği)
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