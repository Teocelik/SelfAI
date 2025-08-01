using Iyzipay.Model;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.DTOs.IyzicoPaymentDtos;
using SelfAI.Services.Interfaces;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using Iyzipay.Request;
using Iyzipay;
using SelfAI.Models.Payment;
using Iyzipay.Model.V2.Subscription;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SelfAI.Services.Concretes
{
    public class PaymentService : IPaymentService
    {
        //Field
        private readonly IyzicoOptions _options;

        public PaymentService(IOptions<IyzicoOptions> options)
        {
            _options = options.Value;
        }

        //Ödeme yapmak için Iyzico'nun checkOut Formunu döndürür.
        public async Task<CheckoutFormInitialize> CheckoutFormInitializeAsync()
        {

            Iyzipay.Options iyziOptions = new Iyzipay.Options
            {
                ApiKey = _options.ApiKey,
                SecretKey = _options.SecretKey,
                BaseUrl = _options.BaseUrl,
            };


            var request = new CreateCheckoutFormInitializeRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                Price = "1.0",
                PaidPrice = "1.2",
                Currency = Currency.TRY.ToString(),
                BasketId = "B67832",
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = "https://localhost:44305/Payment/PayCallBack", // Ödeme sonrası geri dönüş URL'si

            };

            request.Buyer = new Buyer
            {
                Id = "BY789",
                Name = "John",
                Surname = "Doe",
                Email = "email@email.com",
                IdentityNumber = "74300864791",
                GsmNumber = "+905350000000",
                RegistrationAddress = "Nidakule Göztepe",
                City = "Istanbul",
                Country = "Turkey",
                Ip = "85.34.78.112"
            };

            request.ShippingAddress = new Address
            {
                ContactName = "John Doe",
                City = "Istanbul",
                Country = "Turkey",
                Description = "Nidakule Göztepe",
                ZipCode = "34742"
            };

            request.BillingAddress = request.ShippingAddress;

            request.BasketItems = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = "BI101",
                    Name = "Binocular",
                    Category1 = "Electronics",
                    Category2 = "Optics",
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = "1.0"
                }
            };

            return await CheckoutFormInitialize.Create(request, iyziOptions);
        }

        //
        public async Task<CheckoutForm> CallBackResultAsync(IyzicoCallBackDataDto callBackResul)
        {
            Iyzipay.Options iyziOptions = new Iyzipay.Options
            {
                ApiKey = _options.ApiKey,
                SecretKey = _options.SecretKey,
                BaseUrl = _options.BaseUrl
            };

            var request = new RetrieveCheckoutFormRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = callBackResul.ConversationId,
                Token = callBackResul.Token
            };

            // Iyzico'dan gelen token ile ödeme formunu alıyoruz
            var checkoutForm = await CheckoutForm.Retrieve(request, iyziOptions);

            // Ödeme formunun durumunu kontrol ediyoruz
            if(checkoutForm.Status != "success")
            {
                throw new Exception("Ödeme alınamadı: " + checkoutForm.ErrorMessage);
            }

            return checkoutForm; // Ödeme formunu döndürüyoruz
        }
    }
}
