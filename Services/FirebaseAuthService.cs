using SelfAI.Models;
using Microsoft.Extensions.Options;
namespace SelfAI.Services
{
    public class FirebaseAuthService
    {
        private readonly FireBaseAuth _fireBaseAuth;
        private readonly HttpClient _httpClient;
        public FirebaseAuthService(HttpClient httpClient ,IOptions<FireBaseAuth> options)
        {
            _httpClient = httpClient;
            _fireBaseAuth = options.Value;
        }
    }
}
