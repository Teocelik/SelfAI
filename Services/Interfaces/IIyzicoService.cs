using SelfAI.Models;

namespace SelfAI.Services.Interfaces
{
    // Iyzico CheckoutForm başlatma sonucu — checkout sayfasına yönlendirme için gerekli alanlar.
    public class IyzicoCheckoutInitiateResult
    {
        public string Token { get; set; } = string.Empty;
        public string PaymentPageUrl { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
    }

    // Callback'te token ile çekilen ödeme sonucu.
    public class IyzicoPaymentResult
    {
        public bool IsSuccess { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface IIyzicoService
    {
        // Subscribe akışında: Iyzico hosted checkout sayfası URL'i + token döner.
        Task<ServiceResult<IyzicoCheckoutInitiateResult>> InitiateCheckoutAsync(
            Guid subscriptionId,
            string conversationId,
            decimal amount,
            string packageName,
            string userEmail,
            string callbackUrl);

        // Callback'te: Iyzico'nun gönderdiği token ile ödeme sonucunu çeker.
        Task<ServiceResult<IyzicoPaymentResult>> RetrievePaymentResultAsync(string token);
    }
}
