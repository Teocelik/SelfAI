# CLAUDE.md — SelfAI

Bu dosya, Claude Code'un SelfAI projesinde çalışırken takip etmesi gereken kuralları, mimari kararları ve bağlam bilgisini içerir. Yeni bir görev geldiğinde **önce bu dosyayı oku, sonra ilgili kaynak dosyaları aç**.

---

## 1. Proje özeti ve vizyon

**SelfAI**, RenderNet (yeni adıyla **Affogato**) API'sini kullanan bir **ASP.NET Core 9.0 MVC** web uygulamasıdır.

### Nihai vizyon (hedef)
Kullanıcı şu akışı yaşar:
1. **Referans bir görsel yükler** (yüz, sahne, nesne fark etmez).
2. **Bir prompt yazar** veya **hazır prompt'lardan birini seçer**.
3. **Üretim tipini seçer**: AI görsel veya AI video.
4. **Model seçer** (Flux, JuggernautXL vs.).
5. Sonuç hazır olunca SignalR ile bildirim alır ve önizler/indirir.

Yani proje sadece görsel üretimi değil, **görsel + video** üretimini destekleyecek tek bir platform olacak. Ekstra özellikler (karakter sistemi, TrueTouch upscale, çoklu görsel batch işleme vs.) ilk sürümden sonra eklenecek.

### Mevcut durum
- **Görsel üretimi (FaceLock ile):** Çalışan akışın temel iskeleti var ama bazı parametreler hardcoded, bazı endpoint'ler eksik.
- **Video üretimi (Video Anyone):** Henüz başlanmamış. RenderNet'in `video_anyone` parametresi ile yapılacak.
- **Hazır prompt sistemi:** **Var ama backend'e bağlı değil**, mevcut frontend UX'i korunarak backend'e bağlanacak. Mevcut durum:
  - Frontend (`prompt-handler.js`) bir "Surprise me" tarzı buton içeriyor — kodda hardcoded duran 10 İngilizce prompt'tan **rastgele birini** textarea'ya yazıyor.
  - Backend (`Services/Concretes/PromptService.cs`) 5 Türkçe prompt'lık statik bir liste içeriyor ama DI'da kayıtlı değil, hiçbir yerde enjekte edilmiyor, kullanılmıyor — şu an dead code.
  - İki taraf birbirinden habersiz, dilleri bile farklı.
  - **Hedef:** Frontend'in mantığı (random seçim, tek buton) **aynen korunacak**. Tek değişiklik: prompt listesi artık JS'te hardcoded olmayacak, backend'deki PromptService'den fetch ile gelecek. Yani UX değişmez, sadece "tek doğruluk kaynağı" backend'e taşınır. Hata yönetimi mevcut Toast/apiFetch yapısıyla uyumlu olacak.
- **Auth + ödeme:** Iyzico iskeleti var, Firebase Auth henüz tamamlanmamış.

### Kararlar (kilitli — değiştirme önerme)
- **Auth modeli:** Email-only Firebase Auth (passwordless, doğrulama e-postası ile giriş). Şifre yok, sadece email + verification link akışı. Bu kalıcı sistem, geçici değil.
- **clientId → userId geçişi:** Auth devreye girince mevcut localStorage tabanlı `selfai_client_id` mekanizması Firebase'in `userId` değerine bağlanacak. O zamana kadar tarayıcı/incognito değişince pending sonuçların kaybolması **kabul edilebilir bir geçici durum**.
- **Geliştirme prensibi:** "Yapıyı bozmadan eksikleri tamamla." Mevcut mimari kalıplar (ServiceResult, IOptions, typed HttpClient, IIFE modüller, global exception middleware, SignalR+polling akışı) **dokunulmaz**. Yeni özellikler bu kalıplara uyarak eklenir.
- **Test kısıtı:** Şu an RenderNet'in ücretli versiyonu yok, gerçek bir görsel üretim çağrısı **yapılamıyor**. Bu yüzden:
  - "API'ye gerçek istek atıp sonucu görelim" gibi test adımları **önerme**.
  - Bunun yerine: derleme (`dotnet build`), uygulama başlatma (`dotnet run`), config doğrulama (User Secrets list, log satırı), DTO/JSON serialization unit test gibi **offline doğrulama** yolları öner.
  - API'den dönen response gerektiğinde dokümantasyondaki örnek payload'ları kullan (`https://docs.rendernet.ai/llms.txt` index'ten ilgili endpoint sayfasına git).
- **Ödeme stratejisi:** İki sağlayıcı paralel tutuluyor — **Iyzipay** (Türkiye, aktif) ve **Stripe.net** (global, ileride yapılandırılacak). Stripe.net paketi kurulu olmasına rağmen şu an aktif olarak kullanılmıyor, ama **silinmemeli**; global ödeme için saklanıyor. "Kullanılmayan paket var, sileyim mi" diye önerme.
- **MVC katman sorumlulukları (sıkı):**
  - **Controller**: yalnızca HTTP/transport katmanı işleri. Header/query/route validation, model binding, status code seçimi, ServiceResult → HTTP response dönüşümü, [Authorize]/header guard clause'ları. **İş mantığı YOK.**
  - **Service**: tüm iş mantığı. Veri doğrulama (business rule), karar verme, dış API entegrasyonu, polling/job koordinasyonu, default değer atama. Servis hiçbir zaman HTTP'yi bilmemeli (HttpContext inject etme, Request/Response objelerine erişme).
  - **DTO**: ham veri taşıyıcı. Mantık veya method içermez, varsayılan değerler (CfgScale=7.0 gibi) tamam ama davranış değil.
  - Bu sınırı ihlal eden öneri yapma. Şüphedeysen "bu controller'a mı service'e mi ait" diye sor.
  - - **Üretim ortamı kuyruğu (gelecek planı):** Uygulama yayına alındığında eş zamanlı istek yükünü yönetmek için **AWS SQS** ile istek kuyruğa alma sistemi eklenecek. Şu anki mimari (Controller → Service → direkt RenderNet API çağrısı) tek geliştirici testleri için yeterli, ama prod'da SQS producer/consumer pattern'i geçecek. Bu yüzden:
  - Yeni iş mantığı eklerken Controller ile Service arasında temiz bir sınır koru — Service'in dış API'yi çağırma adımı ileride SQS consumer worker'ına taşınabilmeli.
  - Polling job mantığı (GenerationPollingService) zaten generation_id bazlı çalıştığı için SQS sonrası aynı kalabilir — değiştirme önerme.
  - SQS'e karşı alternatif kuyruk sistemleri (RabbitMQ, Hangfire, Azure Service Bus) ÖNERME — karar verildi.

Ödeme entegrasyonu **Iyzico** (Türkiye'de kredi satışı için), kimlik doğrulama planı **Firebase**'dir. Üretim asenkron olduğu için arka planda **polling + SignalR** ile sonuç kullanıcıya push edilir.

Varsayılan route: `{controller=RenderNet}/{action=Index}/{id?}` — yani uygulama açıldığında `RenderNetController.Index()` çalışır.

---

## 2. Teknoloji yığını

**Backend**
- .NET 9.0 (Nullable enabled, ImplicitUsings enabled)
- ASP.NET Core MVC + SignalR (`Microsoft.AspNetCore.SignalR`)
- `System.Text.Json` (varsayılan); bazı dosyalarda `Newtonsoft.Json` import edilmiş ama aktif kullanılmıyor — **yeni kodda `System.Text.Json` kullan**
- Entity Framework Core 9.0.6 (kurulu ama henüz DbContext yok)
- FirebaseAuthentication.net 4.1.0 (kurulu, henüz entegre değil)
- Iyzipay 2.1.67 (aktif ödeme entegrasyonu)
- Stripe.net 48.2.0 (kurulu ama kullanılmıyor — silinebilir veya ileride aktive edilecek)

**Frontend**
- Razor Views (.cshtml)
- TailwindCSS 3.4.17 (`./css/tailwind.css` → `./css/main.css` derlenir)
- Vanilla JavaScript (modül başına bir IIFE — React/Vue YOK)
- SignalR Client (CDN üzerinden, v8.0.0)
- Font Awesome 6.4.0 (CDN)
- Firebase JS SDK (auth callback dosyaları mevcut)
- `@dhiwise/component-tagger` (Tailwind build pipeline'ında)

**Dış servisler**
- **RenderNet / Affogato API**: AI görsel üretimi (base URL: `https://api.rendernet.ai/pub/v1`)
- **Iyzico API**: ödeme (sandbox: `https://sandbox-api.iyzipay.com`, prod: `https://api.iyzipay.com`)

---

## 3. Dizin yapısı

```
/
├── BackgroundServices/        # Arka plan servisleri (IHostedService)
│   └── GenerationPollingService.cs   # Generation durumunu periyodik kontrol eder, sonucu SignalR ile push eder
├── Configurations/            # IOptions pattern config sınıfları
│   ├── RenderNetOptions.cs           # ApiKey + BaseUrl
│   └── IyzicoOptions.cs              # ApiKey + SecretKey + BaseUrl
├── Controllers/               # MVC controller'ları
│   ├── RenderNetController.cs        # Ana controller (image generation, asset upload, flux styles)
│   ├── PaymentController.cs          # Iyzico ödeme flow
│   ├── AccountController.cs          # ⚠️ Boş stub (auth henüz yok)
│   └── HomeController.cs
├── DTOs/                      # API request/response DTO'ları, alt klasörlerde ayrılmış
│   ├── RenderNetGenerationRequestDtos/   # MediaGenerationRequestDto
│   ├── RenderNetGenerationResponseDtos/  # GenerateMediaResponseDto, GetGenerationResponseDto
│   ├── RenderNetUploadResponseDtos/      # UploadAsset* DTO'ları
│   ├── RenderNetCharacterResponseDtos/   # Character DTO'ları
│   ├── RenderNetResourceDtos/            # FluxImageStyle DTO'ları
│   ├── RenderNetGenerationDto/           # ⚠️ Boş klasör (csproj'da tanımlı)
│   └── IyzicoPaymentDtos/                # CreateCheckOutFormRequestDto, IyzicoCallBackDataDto
├── Hubs/
│   └── GenerationHub.cs              # SignalR hub — endpoint: /generationHub
├── Middlewares/
│   └── ExceptionHandlingMiddleware.cs  # Global exception handler (AJAX→JSON, normal→/Home/Error)
├── Models/
│   ├── ServiceResult.cs              # Generic ServiceResult<T> pattern
│   ├── ErrorViewModel.cs
│   └── Payment/                      # Iyzico request/result modelleri
├── Services/
│   ├── Interfaces/                   # IRenderNetAssetService, IRenderNetGenerationService,
│   │                                 #   IRenderNetCharacterService, IRenderNetResourcesService,
│   │                                 #   IPaymentService
│   └── Concretes/                    # ↑ implementasyonlar + PromptService (stub)
├── ViewModels/
├── Views/
│   ├── RenderNet/Index.cshtml        # Ana üretim UI'ı (772 satır, tek sayfa)
│   ├── Payment/IyzicoCheckOutForm.cshtml
│   ├── Home/, Shared/, Account/ (boş)
├── wwwroot/
│   ├── css/                          # tailwind.css (kaynak), main.css (derlenmiş), site.css, toast.css, input.css
│   ├── js/
│   │   ├── app.js                    # 🎯 Ana orchestrator — SignalR setup, clientId, form submit
│   │   ├── toast.js                  # Bildirim sistemi (Toast.success/error/warning/info)
│   │   ├── face-lock-panel.js        # 1252 satır — yüz fotoğrafı yükleme paneli
│   │   ├── flux-image-styles.js      # Flux stil seçimi
│   │   ├── flux-models-loader.js     # Flux modellerini yükler
│   │   ├── model-selection-panel.js  # Model seçim UI
│   │   ├── prompt-handler.js         # Prompt input + validation
│   │   ├── image-controls.js         # Görsel preview ve state yönetimi
│   │   ├── firebase-*.js             # Firebase auth scriptleri (entegre değil)
│   │   └── site.js                   # Boş
│   └── images/, lib/
├── Properties/launchSettings.json    # http: 5175, https: 7117
├── Program.cs                        # DI registration + middleware pipeline
├── SelfAI.csproj
├── appsettings.json                  # Boş template
├── appsettings.Development.json      # Şu an demo/sandbox anahtarları — gerçek anahtarlar için User Secrets kullanılacak
├── appsettings.Production.json       # Şu an .gitignore'da
├── package.json                      # Tailwind/PostCSS build scripts
└── tailwind.config.js
```

---

## 4. RenderNet (Affogato) API ile entegrasyon

**ÖNEMLİ:** RenderNet artık **Affogato** olarak yeniden adlandırıldı. Dokümantasyon hâlâ `docs.rendernet.ai` adresinde, ama proje kodunda "RenderNet" ismini koruyoruz — refactor yapılırsa breaking change olur. Yeni kodda yine RenderNet ismini kullan.

### 4.1 Yetkilendirme
Tüm endpoint'ler `X-API-KEY` header'ı gerektirir. Bu header her HttpClient'in constructor'ında set ediliyor:

```csharp
_httpClient.BaseAddress = new Uri(_settings.BaseUrl);
_httpClient.DefaultRequestHeaders.Add("X-API-KEY", _settings.ApiKey);
```

### 4.2 Görsel üretim flow'u (kritik — anla ve koru!)

API **asenkron** çalışır. Tek bir generation şu adımlardan geçer:

```
[Frontend]                              [Backend]                              [RenderNet API]
    │                                       │                                       │
    │── POST /RenderNet/GenerateImage ──────▶                                       │
    │     (headers: X-Client-Id,            │                                       │
    │      X-SignalR-ConnectionId)          │                                       │
    │                                       │── POST /pub/v1/generations ──────────▶
    │                                       │                                       │
    │                                       │◀── {generation_id, status:initiated}──│
    │◀── 200 {generationId} ────────────────│                                       │
    │                                       │                                       │
    │                                       │   [GenerationPollingService]          │
    │                                       │   3 saniyede bir, max 60 deneme       │
    │                                       │── GET /pub/v1/generations/{id} ──────▶│
    │                                       │◀── {media:[{status, url?}]} ──────────│
    │                                       │                                       │
    │                                       │   (status: initiated/processing/      │
    │                                       │    success/failed)                    │
    │                                       │                                       │
    │◀── SignalR: GenerationCompleted ──────│ (status=success olunca)               │
    │     {generationId, media:[{url}]}     │                                       │
```

**Önemli noktalar:**
- `POST /generations` body'si **bir array** — tek istek için bile `[ { ... } ]` gönderilir.
- Polling sırasında media status değerleri: `initiated`, `processing`, `success`, `failed`. URL yalnızca `success` durumunda dolu gelir.
- `cfg_scale` aralığı: 4–12 (ideal). Steps: 10–30. `aspect_ratio` enum: `1:1, 2:3, 3:2, 4:5, 16:9, 9:16`.
- `quality` enum (case-sensitive!): `Plus`, `Regular`. Kod şu an "Standard" gönderiyor — bu **dokümana göre geçersiz**, kontrol edilmeli.
- `sampler` enum (case-sensitive!): `DPM++ 2M Karras`, `DPM++ 2M SDE Karras`, `DPM++ 2S a Karras`, `DPM++ SDE`, `DPM++ SDE Karras`, `Euler a`.
- Character objesi için `weight` ve `enable_facelock` **deprecated** — yeni kodda yalnızca `mode` (`flexible`/`balanced`/`strong`) kullan.
- Tek bir generation objesinde **şu alanlardan yalnızca biri** olmalı: `segment`, `style`, `model`, `true_touch`, `narrator`, `video_anyone` (öncelik sırası bu yönde).
- **NSFW/şiddet/kamu figürü içerik üretme girişimi hesabın kalıcı olarak askıya alınmasına yol açar.** Negative prompt'ta zaten "nsfw, deformed..." var, kullanıcı pozitif prompt'una bu tür içerik yazarsa backend'de filtrele.

### 4.3 Asset upload flow'u (iki aşamalı!)

```
1. POST /pub/v1/assets/upload  body: {size:{height,width}}
   → cevap: {data:{asset:{id}, upload_url}}
2. PUT {upload_url}  body: dosya (binary)
   → cevap: 200 (S3'e direkt yükleme)
3. Artık asset.id ile generation isteğinde facelock.asset_id olarak kullanılabilir.
```

Mevcut implementasyon: `Services/Concretes/RenderNetAssetService.cs::GetAssetIdAsync`. Bu metot iki aşamayı da tek çağrıda yapıyor — bozma, sadece düzeltme/optimizasyon gerekirse müdahale et. **Stream `Position = 0`'a sarılıyor** (yeniden okunabilir olması için), bunu kaldırma.

### 4.4 Diğer endpoint'ler

- `GET /pub/v1/styles?type=flux&page=N&page_size=N` — Flux stillerini listeler (`RenderNetResourcesService.GetFluxStylesAsync`).
- `GET /pub/v1/models`, `GET /pub/v1/loras`, `GET /pub/v1/controlnets`, `GET /pub/v1/voices` — kaynak listeleri.
- `POST /pub/v1/characters`, `GET /pub/v1/characters` — karakter yönetimi (proje henüz kullanmıyor; `RenderNetCharacterService` stub).
- `GET /pub/v1/assets`, `POST /pub/v2/assets/upload` (video/audio için v2) — asset listesi ve yeni upload.

Yeni endpoint eklerken **mutlaka önce dokümantasyona bak**: `https://docs.rendernet.ai/llms.txt` index'i içerir.

---

## 5. Mimari kurallar ve kalıplar (DİKKAT)

### 5.1 ServiceResult\<T> pattern
Servis katmanı **asla exception throw etmez**, her zaman `ServiceResult<T>` döner. Controller bunu kontrol edip uygun HTTP cevabını üretir.

```csharp
return ServiceResult<MyDto>.Success(data, "Başarılı mesaj");
return ServiceResult<MyDto>.Failure("Kullanıcıya gösterilecek güvenli mesaj", 502);
```

**Asla** API hatasının ham mesajını kullanıcıya gösterme. Detay log'a yazılır, kullanıcıya Türkçe + güvenli mesaj döner. Mevcut servislerde bu prensip tutarlı şekilde uygulanmış, koruman gerekiyor.

### 5.2 IOptions pattern
Konfigürasyon `Program.cs`'de `Configure<TOptions>` ile bağlanır, servislerde `IOptions<TOptions>` enjekte edilir. **Hardcode API key/URL yazma.** Yeni bir dış servis eklersen yeni bir `XOptions` sınıfı yarat.

### 5.3 HttpClient enjeksiyonu
Her dış servis için `AddHttpClient<TInterface, TImpl>()` ile typed HttpClient kayıtlı. Yeni dış servisler için aynı kalıbı izle — `HttpClientFactory`'yi direkt kullanma, `HttpClient`'ı `new`'leme.

### 5.4 JSON serialization
- **Yeni kodda `System.Text.Json` kullan.** Newtonsoft sadece bazı dosyalarda import edilmiş, aktif değil.
- RenderNet API yanıtları `snake_case` — DTO'larda `[JsonPropertyName("snake_case")]` attribute kullan.
- Deserialize ederken `PropertyNameCaseInsensitive = true` set et (yerleşik kalıp).

### 5.5 Logging
Yapılandırılmış logging zorunlu, **pipe-separated key-value formatı** kullanılıyor:

```csharp
_logger.LogInformation(
    "Görsel oluşturma isteği başlatıldı. | Model: {Model} | AspectRatio: {AspectRatio} | BatchSize: {BatchSize}",
    dto.Model, dto.AspectRatio, dto.BatchSize);
```

Bu formatı koru. Tüm log mesajları **Türkçe**.

### 5.6 SignalR + Polling mimarisi
Generation tamamlanmasını beklemek için iki katmanlı bir sistem var:

- **`GenerationPollingService`** (Singleton + HostedService): `ConcurrentDictionary` ile aktif job'ları tutar, 3 saniyede bir RenderNet'e sorar.
- **`GenerationHub`**: SignalR hub'ı. Frontend `RegisterClient(clientId)` çağırır, backend connectionId ile eşleştirir.
- **`_pendingResults`**: Kullanıcı offline olduğunda tamamlanan sonuçları 24 saat saklar. Frontend geri döndüğünde `RegisterClient` çağırır → bekleyen sonuçlar gönderilir.
- **`clientId`** localStorage'da `selfai_client_id` anahtarıyla saklanır. Format: `client_{UUID}`. Auth eklenince `userId` ile değiştirilebilir.

**Bu mimarinin önemli bir özelliği:** SignalR bağlantısı kopsa bile polling devam eder ve sonuç pending'e yazılır. Bu davranışı **bozma**; reconnect senaryosu için kritik.

### 5.7 Frontend modül kalıbı
Her JS dosyası IIFE pattern ile bir modül export eder:

```javascript
const ModuleName = (function () {
    'use strict';
    function init() { /* ... */ }
    return { init, /* public API */ };
})();
```

Modüller arasında bağımlılık `App.js`'teki `init*` fonksiyonlarıyla kurulur. **ES modülleri, bundler veya framework KULLANMA** (React, Vue, Webpack, Vite vs.). Mevcut yapı kasıtlı olarak basit tutulmuş.

Toast bildirimleri: `Toast.success(msg, title)`, `Toast.error(msg, title)`, `Toast.warning(...)`, `Toast.info(...)`. API çağrıları için `apiFetch(url, options)` helper'ı (toast.js'te tanımlı) kullan, fetch'i direkt çağırma — global hata handler buna bağlı.

### 5.8 Global exception middleware
`Program.cs`'in ilk satırında `app.UseGlobalExceptionHandling()` ile bağlı. AJAX isteklerine JSON, normal isteklere `/Home/Error` redirect döner. **Her controller'a try-catch yazmana gerek yok**, beklenmeyen hataları bu yakalar. Ancak servis katmanı `ServiceResult.Failure` ile **kendi hata cevabını** dönmeli — middleware son çare.

---

## 6. Geliştirme komutları

### Backend
```bash
dotnet restore                    # NuGet paketlerini geri yükle
dotnet build                      # Derle
dotnet run                        # Çalıştır (https://localhost:7117, http://localhost:5175)
dotnet run --launch-profile https # HTTPS profilinde
```

### Frontend (Tailwind)
```bash
npm install                       # Bağımlılıkları kur
npm run build:css                 # Tek seferlik build
npm run watch:css                 # Dosyaları izle, otomatik rebuild (geliştirme sırasında çalıştır)
npm run dev                       # watch:css alias
```

### User Secrets (geliştirme — API key'leri için tek doğru yol!)
```bash
dotnet user-secrets init
dotnet user-secrets set "RenderNetOptions:ApiKey" "yeni-anahtarın"
dotnet user-secrets set "RenderNetOptions:BaseUrl" "https://api.rendernet.ai/pub/v1"
dotnet user-secrets set "IyzicoOptions:ApiKey" "sandbox-..."
dotnet user-secrets set "IyzicoOptions:SecretKey" "sandbox-..."
dotnet user-secrets set "IyzicoOptions:BaseUrl" "https://sandbox-api.iyzipay.com"
```

---

## 7. Bilinen eksiklikler / TODO

- **`AccountController`** boş — kimlik doğrulama (muhtemelen Firebase Auth) henüz yok.
- **`RenderNetCharacterService`** sadece constructor; karakter create/edit/list metotları yok.
- **`PromptService`** sadece 5 elemanlı statik Türkçe prompt listesi içeriyor; DI'a kayıtlı değil, hiçbir yerde kullanılmıyor — backend servisine dönüştürülecek (interface + DI kaydı + endpoint), frontend "Surprise me" butonu bu endpoint'ten fetch edecek. Frontend mantığı (random seçim) **aynen korunacak**.
- **`DTOs/RenderNetGenerationDto/`** boş klasör, csproj'da tanımlı.
- **`Views/Account/`** boş klasör.
- **EF Core** kurulu ama DbContext yok — veritabanı katmanı henüz başlamadı.
- **Stripe.net** paketi kurulu ama hiç kullanılmıyor.
- `RenderNetGenerationService.GenerateMediaAsync` içinde bazı parametreler **hardcoded** (`cfg_scale=7`, `style="Realistic"`, `steps=25`, `seed=42`, negative prompt). Bunlar DTO'dan geliyor olmalıydı — DTO'da alanları var ama kullanılmıyor. **Refactor önceliği var.**
- `quality = "Standard"` gönderiliyor — RenderNet dokümantasyonu `Plus` veya `Regular` bekliyor. **Bug ihtimali**, doğrulamak gerek.

---

## 8. KESİN YAPILMAMASI gerekenler

1. **API key'leri asla** appsettings.*.json, kaynak kod veya commit mesajına yazma. User Secrets veya env variable kullan.
2. **`GenerationPollingService`'in singleton yaşam döngüsünü değiştirme.** `_activeJobs`, `_pendingResults`, `_clientConnections` aynı instance üzerinde tutuluyor.
3. **`Program.cs`'deki HttpClient registration sırasını bozma** — typed client'lar her servis için ayrı kayıtlı, paylaşılmamalı.
4. **SignalR hub endpoint'ini değiştirme** — `/generationHub`. Frontend buna bağlı.
5. **Frontend'i framework'leştirme** (React/Vue/Svelte) — IIFE modül kalıbı kasıtlı.
6. **Türkçe log/comment'leri İngilizce'ye çevirme** — proje dili Türkçe, tutarlılığı koru.
7. **`ServiceResult` pattern'ini bypass etme** — controller'a exception throw eden servis yazma.
8. **Newtonsoft.Json ekleme** — `System.Text.Json` kullan. Mevcut Newtonsoft import'ları temizlenebilir.
9. **NSFW/şiddet/kamu figürü içerik üreten kod ekleme** — RenderNet bunu yakalarsa hesap kalıcı kapanır.
10. **`AspNetCore.Identity` ekleme** — Firebase Auth planı mevcut, ikisini birden kullanma.

---

## 9. Çalışma tarzı

- **Dil:** Tüm cevaplar, log mesajları, comment'ler **Türkçe**.
- **Rol dağılımı:** Ben (kullanıcı) komut/spec veririm, sen (Claude) kodu yazarsın. Ben kontrol eder ve kritik yerlerde devreye girerim. Yani interview tarzı değil, **pair programming** tarzı — sen yaz, ben gözden geçir.
- **Ama körü körüne yazma:** Bir özellik veya değişiklik istendiğinde, yazmadan önce şunları kısaca açıkla:
  1. **Hangi dosyaları değiştireceksin / oluşturacaksın** (liste halinde).
  2. **Mevcut hangi kalıba uyacaksın** (ServiceResult, IOptions, typed HttpClient, IIFE modül vs.).
  3. **API dokümantasyonu gerekli mi**, gerekiyorsa hangi endpoint'e bakacaksın.
  4. Belirsizlik varsa **soruyla netleştir**, varsayım yapma.
  Bu mini-plan onaylandıktan sonra kodu yaz.
- **Küçük/açık değişiklikler için** (tek satırlık fix, typo, log mesajı düzeltme vs.) mini-plan atlanabilir — direkt yaz.
- **Büyük refactor veya mimari değişiklik** öneriyorsan: önce **mevcut kalıba neden uymadığını** açıkla, alternatif öner, onay iste.
- **Bilmediğin API davranışı varsa uydurma** — dokümana bak: `https://docs.rendernet.ai/llms.txt` index'tir, oradan ilgili endpoint sayfasına git.
- **Test/doğrulama:** Yeni endpoint veya servis eklediğinde, kullanıcının nasıl test edebileceğini (örnek payload, curl komutu, UI adımı) söyle.
- **Yarım iş bırakma:** Bir dosyayı düzenlerken `// TODO`, `// FIXME`, `throw new NotImplementedException()` ekleyeceksen önce sor — gerçekten gerekli mi, yoksa o anda tamamlanabilir mi?
- **Kritik dosyalara dokunmadan önce uyar:** `Program.cs`, `GenerationPollingService.cs`, `GenerationHub.cs`, `ExceptionHandlingMiddleware.cs` — bunlardan birinde değişiklik gerekiyorsa "şu dosyaya şu nedenle dokunacağım, onaylıyor musun?" diye sor.