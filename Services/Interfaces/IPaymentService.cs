using Iyzipay.Model;
using Iyzipay.Request;
using SelfAI.DTOs.IyzicoPaymentDtos;

namespace SelfAI.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<CheckoutFormInitialize> CheckoutFormInitializeAsync();
        Task<CheckoutForm> CallBackResultAsync(IyzicoCallBackDataDto callBackResul);
    }
}
