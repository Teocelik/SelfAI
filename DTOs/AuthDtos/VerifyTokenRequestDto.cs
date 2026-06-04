namespace SelfAI.DTOs.AuthDtos
{
    // Frontend'in Firebase ID token'ını backend'e POST ederken kullandığı request gövdesi.
    public class VerifyTokenRequestDto
    {
        public string IdToken { get; set; }
    }
}
