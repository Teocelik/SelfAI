namespace SelfAI.Configurations
{
    /// <summary>
    /// Admin yetkilendirme konfigürasyonu.
    /// AllowedEmails içindeki email'lere sahip (Firebase ile giriş yapmış) kullanıcılar
    /// "Admin" policy'sinden geçer. User Secrets'a eklenir (production'da env variable).
    ///
    /// NOT (F.7.3): Proje ASP.NET Core Identity KULLANMAZ (Firebase Auth + Cookie).
    /// Bu yüzden Role store/RoleManager yerine, cookie'deki email claim'i bu listeye
    /// karşı kontrol eden bir AuthorizationHandler ile yetki verilir. Bkz.
    /// Authorization/AdminAuthorizationHandler.cs.
    /// </summary>
    public class AdminOptions
    {
        public const string SectionName = "Admin";

        /// <summary>
        /// Admin yetkisi verilecek email listesi.
        /// Örn: ["cteoman79@gmail.com"]
        /// </summary>
        public List<string> AllowedEmails { get; set; } = new();
    }
}
