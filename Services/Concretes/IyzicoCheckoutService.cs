using Microsoft.Extensions.Options;
using SelfAI.Configurations;
using SelfAI.Services.Interfaces;

namespace SelfAI.Services.Concretes
{
    public class IyzicoCheckoutService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IyzicoOptions _options;
        public IyzicoCheckoutService(HttpClient httpClient, IOptions<IyzicoOptions> options)
        {
            
            _httpClient = httpClient;
            _options = options.Value;
        }
    }
}
