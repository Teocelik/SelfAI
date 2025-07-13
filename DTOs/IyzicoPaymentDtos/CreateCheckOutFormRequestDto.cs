using Iyzipay.Model;

namespace SelfAI.DTOs.IyzicoPaymentDtos
{
    public class CreateCheckOutFormRequestDto
    {
        public string ConversationId { get; set; }
        public decimal Price { get; set; }
        public string BuyerEmail { get; set; }
        public string BuyerName { get; set; }
        public string BuyerSurname { get; set; }
        public string BuyerIp { get; set; }
        public string CallbackUrl { get; set; }
        public string BasketId { get; set; }
    }
}
