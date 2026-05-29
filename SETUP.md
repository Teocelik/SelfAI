# SelfAI — Geliştirici Kurulum Rehberi

Bu doküman yeni geliştiriciler için ilk kurulum adımlarını içerir. Dış servis API anahtarları **repo'da tutulmaz** — bunun yerine .NET'in **User Secrets** mekanizması kullanılır (sadece geliştirme makinesi için).

> Not: Production ortamında User Secrets çalışmaz. Production için Environment Variables veya bir secret store (Azure Key Vault vb.) kullanılır.

---

## 1. Ön gereksinimler

- .NET 9 SDK
- Node.js 18+ (Tailwind CSS derlemesi için)
- Git
- Bir IDE (Visual Studio 2022, Rider veya VS Code)

---

## 2. Repoyu klonla ve bağımlılıkları kur

```powershell
git clone https://github.com/Teocelik/SelfAI.git
cd SelfAI

# .NET paketleri
dotnet restore

# Frontend (Tailwind)
npm install
```

---

## 3. User Secrets — API anahtarlarını set et

Uygulama Development ortamında User Secrets'ı **otomatik olarak** okur (`Program.cs`'de ekstra kod gerekmez). Sadece anahtarları lokalinize kaydetmen yeterli.

### 3.1 İlk kurulum

`SelfAI.csproj` zaten `<UserSecretsId>` içeriyor — `dotnet user-secrets init` tekrar çalıştırmana gerek yok. Doğrudan değer set edebilirsin:

```powershell
# Proje kök dizininden
dotnet user-secrets set "RenderNetOptions:ApiKey" "<senin-rendernet-key'in>"
dotnet user-secrets set "IyzicoOptions:ApiKey" "<senin-iyzico-sandbox-api-key'in>"
dotnet user-secrets set "IyzicoOptions:SecretKey" "<senin-iyzico-sandbox-secret-key'in>"
```

### 3.2 Kaydedilmesi gereken anahtarlar

| Anahtar | Açıklama | Zorunlu mu? |
|---|---|---|
| `RenderNetOptions:ApiKey` | RenderNet (Affogato) görsel üretim API anahtarı | Evet |
| `IyzicoOptions:ApiKey` | Iyzico sandbox API anahtarı | Ödeme akışını test edecekseniz |
| `IyzicoOptions:SecretKey` | Iyzico sandbox secret key (gerçekten gizli — kimseyle paylaşma) | Ödeme akışını test edecekseniz |

`BaseUrl` değerleri gizli değil; `appsettings.json` ve `appsettings.Development.json` içinde varsayılan olarak duruyor. Override etmek isterseniz aynı yöntemle set edebilirsiniz:

```powershell
dotnet user-secrets set "RenderNetOptions:BaseUrl" "https://api.rendernet.ai/pub/v1"
dotnet user-secrets set "IyzicoOptions:BaseUrl"    "https://sandbox-api.iyzipay.com"
```

### 3.3 Kayıtlı anahtarları görmek / silmek

```powershell
dotnet user-secrets list                          # tümünü göster
dotnet user-secrets remove "RenderNetOptions:ApiKey"  # tekini sil
dotnet user-secrets clear                         # hepsini sil
```

Secrets bilgisayarda şu klasörde tutulur (commit edilmez):

- Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

`UserSecretsId` değeri `SelfAI.csproj` içinde tanımlı.

---

## 4. Anahtarları nereden alacaksın?

### 4.1 RenderNet (Affogato)

1. https://rendernet.ai adresine git, hesap oluştur.
2. Dashboard → **API Keys** sekmesinden yeni bir anahtar üret.
3. Üretilen değeri `RenderNetOptions:ApiKey` olarak kaydet.
4. **Önemli:** Hesabın ücretsiz tier'ında credit yoksa gerçek görsel üretim çağrısı **başarısız döner**. Geliştirme sırasında genelde derleme + başlatma testi yeterlidir; gerçek API çağrısı yapmadan önce kredi alındığından emin ol.

### 4.2 Iyzico (sandbox)

1. https://sandbox-merchant.iyzipay.com adresinde sandbox hesabı aç (ücretsiz).
2. Dashboard → **Ayarlar → API Anahtarları** sekmesinden API Key ve Secret Key'i kopyala (her ikisi de `sandbox-` ile başlar).
3. `IyzicoOptions:ApiKey` ve `IyzicoOptions:SecretKey` olarak kaydet.
4. Iyzico sandbox akışını test etmek için: https://dev.iyzipay.com/tr/test-kartlari sayfasındaki test kart numaralarını kullan.

---

## 5. Uygulamayı çalıştır

```powershell
# Tailwind'i izleme modunda başlat (ayrı bir terminalde)
npm run watch:css

# Asıl uygulamayı başlat
dotnet run
```

Varsayılan adresler (`Properties/launchSettings.json`):
- HTTPS: https://localhost:7117
- HTTP : http://localhost:5175

Tarayıcı açıldığında doğrudan `RenderNetController.Index()` yüklenir (default route).

### Hızlı doğrulama (gerçek API çağrısı yapmadan)

1. `dotnet build` hatasız geçmeli.
2. `dotnet run` ile uygulama başlamalı, konsolda `GenerationPollingService başlatıldı.` log satırı görünmeli.
3. `dotnet user-secrets list` kayıtlı anahtarları listelemeli (üç tanesi).
4. Tarayıcıda ana sayfa yüklenmeli, sağ üst konsolda `[ClientId] Yeni oluşturuldu: client_...` benzeri bir satır olmalı.

---

## 6. Konfigürasyon önceliği (referans)

.NET konfigürasyon değerleri şu sırayla okunur (sonraki öncekini ezer):

1. `appsettings.json`
2. `appsettings.{Environment}.json` (örn. `appsettings.Development.json`)
3. **User Secrets** (yalnız Development'ta)
4. Environment Variables
5. Command-line arguments

Yani Development ortamında User Secrets `appsettings.*.json`'ı override eder. Production'da User Secrets devre dışıdır; orada Environment Variables veya `appsettings.Production.json` kullan.

---

## 7. Sık karşılaşılan hatalar

**`Object reference not set... RenderNetOptions.ApiKey`**
→ User Secrets'a `RenderNetOptions:ApiKey` set etmemişsindir. Adım 3.1'i tekrar çalıştır.

**`appsettings.Development.json missing` uyarısı yok ama ApiKey boş geliyor**
→ `appsettings.Development.json` artık `.gitignore`'da. Klonladıktan sonra dosyayı kendin oluşturman gerekmiyor; `BaseUrl` zaten `appsettings.json`'da default olarak var. Anahtarları User Secrets'a koyman yeterli.

**Iyzico checkout formu 401 dönerse**
→ SecretKey eksik veya yanlış. `dotnet user-secrets list` ile kontrol et.

---

## 8. İleri okuma

- `CLAUDE.md` — projenin mimari kuralları ve kalıpları
- `README.md` — proje genel özeti
- RenderNet API docs: https://docs.rendernet.ai/llms.txt
- Iyzico sandbox dokümantasyonu: https://dev.iyzipay.com
