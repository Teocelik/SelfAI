namespace SelfAI.Entities.Enums;

/// <summary>
/// Karakter LoRA eğitiminin yaşam döngüsü durumu (F.M.4).
/// DB'de int olarak saklanır (AppDbContext'te HasConversion&lt;int&gt;).
/// </summary>
public enum LoraTrainingStatus
{
    Pending = 0,    // Henüz training başlamadı
    Uploading = 1,  // Yüz görselleri fal.ai storage'a yükleniyor
    Training = 2,   // fal.ai LoRA training in-progress
    Ready = 3,      // Training tamamlandı, generation'da kullanılabilir
    Failed = 4      // Training başarısız (kredi iade edildi)
}
