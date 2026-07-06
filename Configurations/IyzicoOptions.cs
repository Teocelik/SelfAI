namespace SelfAI.Configurations
{
    public class IyzicoOptions
    {
        /// <summary>
        /// Configuration section adı — Program.cs bind'inde kullanılır (magic string yerine).
        /// NOT: Değer "IyzicoOptions" olarak KORUNUR (CLAUDE.md kilitli kararı: section
        /// "IyzicoOptions" olmalı, "Iyzico" DEĞİL). User Secrets anahtarlarıyla uyumlu.
        /// </summary>
        public const string SectionName = "IyzicoOptions";

        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public string BaseUrl { get; set; }
    }
}
