using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SelfAI.DTOs.IyzicoPaymentDtos;
using SelfAI.Services.Interfaces;
using Stripe;

namespace SelfAI.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<IActionResult> Pay()
        {
            // Ödeme işlemini başlatıyoruz
            // Bu işlem, Iyzico API'sine ödeme isteği gönderir ve bir ödeme nesnesi döner
            // Bu ödeme nesnesi, ödeme formunu oluşturmak için kullanılacaktır
            var result = await _paymentService.MakePayment();


            return View("Redirect", result.CheckoutFormContent); // HTML formu basıyoruz
        }

        // iyzico'dan gelen formu otomatik göndereceğimiz sayfa
        public IActionResult Redirect()
        {
            return View(); // Redirect.cshtml içinde @Html.Raw(model)
        }

    }
}
