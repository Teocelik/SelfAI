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

        //[HttpGet]
        public async Task<IActionResult> InitializeIyzicoCheckOutForm()
        {
            // Ödeme işlemini başlatıyoruz
            // Bu işlem, Iyzico API'sine ödeme isteği gönderir ve bir ödeme nesnesi döner
            // Bu ödeme nesnesi, ödeme formunu oluşturmak için kullanılacaktır
            var result = await _paymentService.CheckoutFormInitializeAsync();


            return View("IyzicoCheckOutForm", result.CheckoutFormContent); // HTML formu basıyoruz
        }

        // iyzico'dan gelen formu otomatik göndereceğimiz sayfa
        public IActionResult IyzicoCheckOutForm()
        {
            return View(); // Redirect.cshtml içinde @Html.Raw(model)
        }

        // Ödeme butununa tıklandığında çağrılacak olan action
        [HttpPost]
        public async Task<IActionResult> PayCallBack([FromForm] IyzicoCallBackDataDto request)
        {
            // Ödeme sonucunu işliyoruz
            var result = await _paymentService.CallBackResultAsync(request);

            return Ok();
        }
    }
}
