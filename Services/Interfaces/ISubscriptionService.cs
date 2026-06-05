using SelfAI.Entities;
using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    public interface ISubscriptionService
    {
        // Subscribe akışını başlatır: Subscription (Pending) + Payment (Pending) kayıtlarını oluşturur.
        // Dönen Payment.IyzicoConversationId, Iyzico initiate çağrısında kullanılır.
        Task<ServiceResult<(Subscription Subscription, Payment Payment)>> InitiateSubscriptionAsync(
            Guid userId, int packageId);

        // Callback'te ödeme başarılıysa çağrılır: Payment + Subscription'ı aktif eder, cüzdana kredi yükler.
        Task<ServiceResult<Subscription>> ActivateSubscriptionAsync(
            string conversationId, string iyzicoPaymentId);

        // Callback'te ödeme başarısızsa veya initiate patlarsa çağrılır: kayıtları iptal eder.
        // Hiçbir cüzdan/kredi hareketi yapmaz.
        Task<ServiceResult<int>> CancelPendingSubscriptionAsync(
            string conversationId, string reason);
    }
}
