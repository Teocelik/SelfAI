namespace SelfAI.DTOs.AuthDtos
{
    // Firebase ID token doğrulandıktan sonra elde edilen kullanıcı bilgilerini taşır.
    public class FirebaseUserInfoDto
    {
        public string Uid { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        public bool EmailVerified { get; set; }
    }
}
