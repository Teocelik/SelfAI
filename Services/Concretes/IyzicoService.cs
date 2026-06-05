using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Models;
using SelfAI.Services.Interfaces;
using System.Globalization;

namespace SelfAI.Services.Concretes
{
    // Iyzico CheckoutForm (hosted) ödeme akışını yöneten servis.
    // PCI uyumu: kart bilgisi Iyzico'nun kendi sayfasında alınır, biz GÖRMEYİZ.
    // Config: mevcut IyzicoOptions (section "IyzicoOptions") yeniden kullanılır —
    //         ApiKey/SecretKey User Secrets'te, BaseUrl appsettings.Development.json'da (sandbox).
    public class IyzicoService : IIyzicoService
    {
        private readonly IyzicoOptions _options;
        private readonly ILogger<IyzicoService> _logger;

        public IyzicoService(IOptions<IyzicoOptions> options, ILogger<IyzicoService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ServiceResult<IyzicoCheckoutInitiateResult>> InitiateCheckoutAsync(
            Guid subscriptionId,
            string conversationId,
            decimal amount,
            string packageName,
            string userEmail,
            string callbackUrl)
        {
            try
            {
                var iyziOptions = BuildOptions();

                // Iyzico fiyat formatı: nokta ayraçlı, en az bir ondalık (örn. "299.0").
                var priceStr = amount.ToString("0.0", CultureInfo.InvariantCulture);

                var request = new CreateCheckoutFormInitializeRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = conversationId,
                    Price = priceStr,
                    PaidPrice = priceStr,
                    Currency = Currency.TRY.ToString(),
                    BasketId = subscriptionId.ToString(),
                    PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
                    CallbackUrl = callbackUrl,

                    // PLACEHOLDER buyer/address bilgileri — gerçek değerler ileride user profile
                    // sprintinde toplanacak. Sandbox bu değerleri kabul eder.
                    Buyer = new Buyer
                    {
                        Id = subscriptionId.ToString(),
                        Name = "Placeholder",
                        Surname = "User",
                        GsmNumber = "+905555555555",
                        Email = userEmail,
                        IdentityNumber = "11111111111",
                        RegistrationAddress = "Placeholder address",
                        Ip = "85.34.78.112",
                        City = "Istanbul",
                        Country = "Turkey",
                        ZipCode = "34000"
                    },
                    ShippingAddress = BuildPlaceholderAddress(),
                    BillingAddress = BuildPlaceholderAddress(),

                    BasketItems = new List<BasketItem>
                    {
                        new BasketItem
                        {
                            Id = subscriptionId.ToString(),
                            Name = packageName,
                            Category1 = "Subscription",
                            ItemType = BasketItemType.VIRTUAL.ToString(),
                            Price = priceStr
                        }
                    }
                };

                _logger.LogInformation(
                    "Iyzico checkout başlatılıyor. | ConvId: {ConvId} | SubId: {SubId} | Tutar: {Tutar} | Paket: {Paket}",
                    conversationId, subscriptionId, priceStr, packageName);

                var result = await CheckoutFormInitialize.Create(request, iyziOptions);

                if (result.Status != "success" || string.IsNullOrWhiteSpace(result.PaymentPageUrl))
                {
                    _logger.LogError(
                        "Iyzico checkout başlatma başarısız. | ConvId: {ConvId} | Status: {Status} | ErrorCode: {ErrorCode} | Error: {Error}",
                        conversationId, result.Status, result.ErrorCode, result.ErrorMessage);
                    return ServiceResult<IyzicoCheckoutInitiateResult>.Failure(
                        "Ödeme sayfası başlatılamadı. Lütfen tekrar deneyin.", 502);
                }

                _logger.LogInformation(
                    "Iyzico checkout başlatıldı. | ConvId: {ConvId} | Token alındı.", conversationId);

                return ServiceResult<IyzicoCheckoutInitiateResult>.Success(new IyzicoCheckoutInitiateResult
                {
                    Token = result.Token,
                    PaymentPageUrl = result.PaymentPageUrl,
                    ConversationId = conversationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Iyzico checkout başlatma sırasında beklenmeyen hata. | ConvId: {ConvId}", conversationId);
                return ServiceResult<IyzicoCheckoutInitiateResult>.Failure(
                    "Ödeme servisine bağlanılamadı. Lütfen daha sonra tekrar deneyin.", 502);
            }
        }

        public async Task<ServiceResult<IyzicoPaymentResult>> RetrievePaymentResultAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ServiceResult<IyzicoPaymentResult>.Failure("Ödeme token'ı eksik.", 400);
            }

            try
            {
                var iyziOptions = BuildOptions();

                var request = new RetrieveCheckoutFormRequest
                {
                    Locale = Locale.TR.ToString(),
                    Token = token
                };

                var form = await CheckoutForm.Retrieve(request, iyziOptions);

                // Status: API çağrısı başarılı mı? PaymentStatus: ödemenin gerçek durumu.
                // İkisi de "başarılı" olmalı. (Non-3DS sandbox'ta PaymentStatus = "SUCCESS")
                var apiOk = string.Equals(form.Status, "success", StringComparison.OrdinalIgnoreCase);
                var paymentOk = string.IsNullOrEmpty(form.PaymentStatus)
                    || string.Equals(form.PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
                var isSuccess = apiOk && paymentOk && !string.IsNullOrEmpty(form.PaymentId);

                var paymentResult = new IyzicoPaymentResult
                {
                    IsSuccess = isSuccess,
                    PaymentId = form.PaymentId ?? string.Empty,
                    ConversationId = form.ConversationId ?? string.Empty,
                    Status = form.PaymentStatus ?? form.Status ?? string.Empty,
                    ErrorMessage = form.ErrorMessage ?? string.Empty
                };

                if (isSuccess)
                {
                    _logger.LogInformation(
                        "Iyzico ödeme sonucu alındı (başarılı). | ConvId: {ConvId} | PaymentId: {PaymentId}",
                        paymentResult.ConversationId, paymentResult.PaymentId);
                }
                else
                {
                    _logger.LogWarning(
                        "Iyzico ödeme sonucu başarısız. | ConvId: {ConvId} | Status: {Status} | PaymentStatus: {PaymentStatus} | Error: {Error}",
                        paymentResult.ConversationId, form.Status, form.PaymentStatus, form.ErrorMessage);
                }

                return ServiceResult<IyzicoPaymentResult>.Success(paymentResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Iyzico ödeme sonucu çekilirken beklenmeyen hata.");
                return ServiceResult<IyzicoPaymentResult>.Failure(
                    "Ödeme sonucu doğrulanamadı. Lütfen destek ile iletişime geçin.", 502);
            }
        }

        private Iyzipay.Options BuildOptions() => new Iyzipay.Options
        {
            ApiKey = _options.ApiKey,
            SecretKey = _options.SecretKey,
            BaseUrl = _options.BaseUrl
        };

        private static Address BuildPlaceholderAddress() => new Address
        {
            ContactName = "Placeholder User",
            City = "Istanbul",
            Country = "Turkey",
            Description = "Placeholder address",
            ZipCode = "34000"
        };
    }
}
