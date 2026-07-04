# CLAUDE.md — SelfAI

Bu dosya, Claude Code'un SelfAI projesinde çalışırken takip etmesi gereken kuralları, mimari kararları ve bağlam bilgisini içerir. Yeni bir görev geldiğinde **önce bu dosyayı oku, sonra ilgili kaynak dosyaları aç**.

---

## 1. Proje özeti ve vizyon

**SelfAI**, **fal.ai** API'sini kullanan bir **ASP.NET Core 9.0 MVC** web uygulamasıdır. fal.ai TEK provider olarak görsel, video, karakter eğitimi, ürün/kıyafet, sosyal medya preset'leri — hepsini sağlar.

### Migration durumu (Affogato → fal.ai)

Daha önce **Affogato/RenderNet** API kullanılıyordu, ancak Affogato API ticari kullanıma izin vermedi. fal.ai'a tam migration sürmektedir. Affogato/RenderNet legacy kodu (servisler, DTO'lar, `RenderNetOptions`) **F.M.8'de tamamen silindi.**

**Migration phase planı:**

- F.M.1 — Foundation refactor (interface skeleton, klasör yapısı, DI registration) — functional değişiklik YOK
- F.M.2 — IFalAiClient HTTP setup + auth + queue/polling/webhook altyapısı
- F.M.3 — Image generation (Flux Dev/Schnell ile basit prompt→image)
- F.M.4 — Character system (Flux LoRA training entegrasyonu, Character entity migration)
- F.M.5 — Multi-model UI bağlama (dinamik catalog, tier-based pricing UI'a yansır)
- F.M.6 — Pose Lock + Face Lock (IP-Adapter / Face-ID / ControlNet entegrasyonu)
- F.M.7 — Asset upload (fal.ai storage entegrasyonu)
- F.M.8 — Affogato legacy kodunu tamamen sil
- F.M.9+ (gelecek) — Video generation, product/clothes try-on, social media presets

### Nihai vizyon (hedef)

Kullanıcı şu akışı yaşar:

1. **Referans bir görsel yükler** (yüz, sahne, nesne fark etmez)
2. **Bir prompt yazar** veya **hazır prompt'lardan birini seçer**
3. **Üretim tipini seçer**: AI görsel, AI video, ürün mockup, kıyafet try-on, sosyal medya preset (TikTok/Instagram)
4. **Model seçer** — fal.ai catalog'undan dinamik liste (Flux Dev/Schnell, Recraft, Seedream, Veo, Kling, vs.)
5. Sonuç hazır olunca SignalR ile bildirim alır ve önizler/indirir

Proje **fal.ai'ın 1000+ modelinden** kategoriye göre filtrelenmiş seçim sunar (görsel üretim barı sadece image modelleri, video barı sadece video modelleri, vb.).

### Mevcut durum

- **Görsel üretimi:** F.M.3'te fal.ai Flux Dev/Schnell ile çalışır hale geliyor
- **Karakter sistemi:** F.M.4'te Flux LoRA training entegrasyonu yapılıyor (eski Affogato karakter sistemi devre dışı, mevcut karakterler `Status='Migrated'` ile gizlendi)
- **Video üretimi:** F.M.9+ ileride. fal.ai'ın Veo, Kling, Seedance modelleri ile yapılacak
- **Hazır prompt sistemi:** Backend'e bağlandı (PromptService aktif, frontend "Surprise me" butonu bu endpoint'ten fetch ediyor)
- **Auth + ödeme:** Firebase Auth + Iyzico + Stripe entegre, credentials User Secrets'ta
- **Credit sistemi:** Kullanıcı paketleri, kredi düşme/refund, top-up tamamlandı
- **Studio UI:** Tamamlandı (model card grid, prompt input, character/face/pose butonları, generate button state, character modal — F.6.5'e kadar tamam)

### Kararlar (kilitli — değiştirme önerme)

- **AI Provider:** fal.ai TEK provider. Başka provider yok. Affogato/RenderNet legacy kodu F.M.8'de tamamen kaldırıldı; yeni kod yalnızca `IFalAiClient` üzerinden gider.
- **Provider abstraction:** `IFalAiClient` (provider-level HTTP) ve `IImageGenerator/IVideoGenerator/ICharacterTrainer` (domain-level) interface'leri ile katmanlı yapı. İleride başka provider (Replicate, Together vs.) eklemek için skeleton hazır. Şu an SADECE fal.ai implementasyonu var.
- **Klasör yapısı (SOLID-uyumlu):** Yeni kod `Services/Generation/Abstractions`, `Services/Generation/Orchestrators`, `Services/Generation/Domain`, `Services/Generation/Providers/FalAi` altına yazılır. Detaylı yapı §3'te.
- **Character entity (fal.ai LoRA):** Eski `AffogatoCharacterId` field'ı F.M.8'de silinir. Yeni field'lar: `LoraModelUrl` (string, fal.ai training output), `LoraTrainingStatus` (enum: Pending/Training/Ready/Failed), `LoraTrainingJobId` (string?), `TrainingStartedAt` (DateTime?), `TrainingCompletedAt` (DateTime?), `FaceReferenceAssetIds` (string[], training input). Mode pills (Esnek/Dengeli/Güçlü) → LoRA weight olarak çevrilir (0.4/0.6/0.8).
- **Affogato karakterleri:** Mevcut DB'de bulunan Affogato karakterleri `Status='Migrated'` işaretlenir, modal'da gösterilmez. Production'a çıkmadığımız için sadece test verisi etkilenir. Migration'a gerek yok (yeni karakterleri kullanıcılar yeniden oluşturur).
- **Asset storage:** Phase 1 fal.ai storage (basit, `fal.storage.upload()` veya REST endpoint). Phase 2 (ileride) S3/R2/Backblaze'e geçiş düşünülecek. Asset entity'de `StorageProvider` field'ı tutulur (gelecek esneklik için).
- **Credit pricing — tier-based markup:** 1 credit = $0.005 USD eşdeğeri. Markup yüzdeleri config'de (`appsettings.json` veya DB):
  - **Fast** (Flux Schnell vb.): 4x markup
  - **Standard** (Flux Dev, Seedream vb.): 2.5x markup
  - **Premium** (Recraft Pro, Flux Pro vb.): 2x markup
  - **CharacterLora** (Flux LoRA inference): 2.5x markup
  - **VideoFast** (Kling v1 vb.): 1.8x markup
  - **VideoPremium** (Veo 3, Sora 2 vb.): 1.5x markup
  
  Formül: `user_credits = ceil((fal_ai_cost_usd × markup_multiplier) / 0.005)`. Markup business kararıyla değişir, kod değişimi gerektirmez.
- **Dynamic model catalog:** Hardcoded model listesi YOK. `IFalAiModelCatalog` servisi fal.ai'dan model listesini fetch eder, kategoriye göre filtreler, markup uygulayıp credit cost hesaplar, IMemoryCache'te tutar (15 dk TTL). Frontend'e geldiğinde Studio'da kart olarak görünür. Yeni modeller catalog refresh ile kendiliğinden eklenir.
- **fal.ai endpoint mapping (genişler):**
  - Image generation → `fal-ai/flux/dev`, `fal-ai/flux/schnell`, `fal-ai/flux-pro`, `fal-ai/recraft-v3`, `fal-ai/seedream`, vb.
  - Character training → `fal-ai/flux-lora-fast-training` (training) + `fal-ai/flux-lora` (inference)
  - Face conditioning → `fal-ai/ip-adapter-face-id` veya `fal-ai/instantid`
  - Pose control → `fal-ai/flux-controlnet` (ControlNet)
  - Video generation (F.M.9+) → `fal-ai/veo-3`, `fal-ai/kling-v2-1`, `fal-ai/seedance-1-5-pro`
  - Product/clothes (F.M.10+) → `fal-ai/idm-vton`, `fal-ai/ace-step`
- **Auth modeli:** Email-only Firebase Auth (passwordless, doğrulama e-postası ile giriş). Şifre yok, sadece email + verification link akışı. Bu kalıcı sistem.
- **clientId → userId geçişi:** Auth devreye girdi, Firebase `userId` artık SignalR client mapping için kullanılıyor. Eski `selfai_client_id` localStorage mekanizması geçiş döneminde uyumluluk için saklı (anonymous fallback senaryoları).
- **Geliştirme prensibi:** "Yapıyı bozmadan eksikleri tamamla." Mevcut mimari kalıplar (ServiceResult, IOptions, typed HttpClient, IIFE modüller, global exception middleware, SignalR+polling akışı) **dokunulmaz**. Yeni özellikler bu kalıplara uyarak eklenir.
- **fal.ai test stratejisi:** Paid hesap aktif, API key User Secrets'ta. Gerçek generation çağrısı yapılabilir AMA her çağrı credit harcar — test verisi hassas olarak seçilmeli. Geliştirme sırasında küçük model + düşük çözünürlük tercih edilir (Flux Schnell, 512×512). Tek API key tüm SelfAI kullanıcılarını besler (reselling modeli — bkz. Credit modeli kararı).
- **Ödeme stratejisi:** İki sağlayıcı paralel aktif — **Iyzipay** (Türkiye, TL ödemeler) ve **Stripe.net** (global, USD ödemeleri). İkisi de canlı kullanımda, hangisi tercih edileceği kullanıcının lokasyonuna/seçimine göre değişir.
- **MVC katman sorumlulukları (sıkı):**
  - **Controller**: yalnızca HTTP/transport katmanı işleri. Header/query/route validation, model binding, status code seçimi, ServiceResult → HTTP response dönüşümü, [Authorize]/header guard clause'ları. **İş mantığı YOK.**
  - **Orchestrator**: yüksek seviye iş akışı koordinasyonu. Model seçimi, kredi düşme, history kaydı, downstream service çağrıları. ServiceResult döner.
  - **Domain Service** (IImageGenerator, IVideoGenerator vs.): tek bir model/kategori için generation iş mantığı. Provider client'ı kullanır.
  - **Provider Client** (IFalAiClient): düşük seviye HTTP iletişim. Tek sorumluluğu fal.ai API ile konuşmak.
  - **DTO**: ham veri taşıyıcı. Mantık veya method içermez.
  - Bu sınırı ihlal eden öneri yapma. Şüphedeysen "bu controller'a mı orchestrator'a mı service'e mi ait" diye sor.
- **SOLID prensiplerinin uygulanışı:**
  - **S** (Single Responsibility): Her interface tek iş — `IImageGenerator` sadece image, `IVideoGenerator` sadece video, `ICharacterTrainer` sadece LoRA training.
  - **O** (Open/Closed): Yeni model = yeni class implementing ilgili interface. Mevcut kod değişmez. Yeni provider eklemek = yeni `IFalAiClient` benzeri implementation.
  - **L** (Liskov): Tüm `IImageGenerator` implementations interchangeable, aynı kontratı doldurur.
  - **I** (Interface Segregation): Mega-interface yok. Controller sadece ihtiyacı olan abstraction'ı alır.
  - **D** (Dependency Inversion): Tüm bağımlılıklar interface'lere. Hiçbir Controller/Orchestrator concrete `FalAiClient`'a doğrudan bağlanmaz.
- **Kredi modeli (API reselling — subscription + quota):** SelfAI fal.ai API'sini wrap eder ve kendi paketler hâlinde satar. Tek bir fal.ai prepaid credit havuzu (örn. $500 yüklenmiş) tüm kullanıcılar tarafından paylaşılır; SelfAI içinde her kullanıcının kendi `TokenWallet`'ı vardır. Paketler: **Free=5 credit (one-time bonus, yenilenmez)**, Basic/Plus/Premium (aylık subscription, yenilenir). Her generation kullanıcının cüzdanından düşülür, fal.ai çağrısı sonucuna göre commit veya refund. Aşırı satım riski admin yönetir (toplam aktif subscription kredisi ≤ fal.ai havuz kapasitesi — admin paneli ileride bunu izleyecek). **Identity sistemi: Firebase Auth, ASP.NET Identity YOK.** `AppUser` kendi domain entity'mizdir, `FirebaseUid` ile bağlanır.
- **Iyzico configuration section:** Iyzico key'leri User Secrets'te `"IyzicoOptions"` section'ı altında saklanır, `"Iyzico"` DEĞİL. Service tarafında `IyzicoOptions` POCO class'ı `IOptions<IyzicoOptions>` ile inject edilir. Yeni bir `IyzicoSettings` class'ı YARATILMAYACAK.
- **Iyzico callback idempotency:** Iyzico callback'i ağ veya kullanıcı kaynaklı olarak iki kez gelebilir. `SubscriptionService.ActivateSubscriptionAsync`, `Payment.Status`'unu önce kontrol eder; zaten `Completed` ise sessizce idempotent çıkış yapar (çift wallet top-up YOK). Status kontrolü hem Iyzico API response status'u (`success`) hem payment status'u (`SUCCESS`) için ayrı ayrı yapılır.
- **Frontend MVC separation (kesin kural):** Razor view'lar (.cshtml) yalnızca markup içerir. JavaScript logic'i `wwwroot/js/{feature}.js` altında ayrı dosyalarda yaşar (IIFE module pattern). CSS kuralları `wwwroot/css/` altında veya `tailwind.css` source'unda `@layer components` içinde tutulur. View'larda `<script>` bloğu içinde logic YASAK; `<style>` bloğu YASAK; inline event handler (onclick=, onchange= vb.) YASAK. `@section Scripts` yalnızca harici dosyayı referans almak için kullanılır. Yeni frontend görevlerinde bu kurala kesinlikle uyulur.
- **Tasarım dili: Liquid Glass / Studio Dark:** Tüm SelfAI UI tutarlı tasarım dili kullanır. Studio bar elementleri "Studio Dark" (flat, az blur, single border) modeline uygun. Modaller ve kart elementler "Liquid Glass" (frosted glass, multi-layered shadows, refractive edges). Dark background varsayılan, accent rengi #00CED1.
- **Kilitli design token kararları:** (1) Tek accent rengi `#00CED1`. (2) Tek page background `#0A0A0A`. (3) Subtle radial gradient: `radial-gradient(ellipse at top, rgba(0, 206, 209, 0.05), #0A0A0A 60%)`. (4) Glass surface alpha: beyaz/nötr alpha (rgba(255,255,255,X)); turkuaz tint yalnızca aktif/seçili öğelerde. (5) Border solid gerektiğinde `rgba(255, 255, 255, 0.12)`. (6) Tailwind strategy: paralel `glass-*` class'ları, eski class'lar fırsat buldukça migrate edilir. (7) Inter font weight 700 import edilir. (8) Focus state: `:focus-visible` ile 2px ring `rgba(0, 206, 209, 0.5)` + glow.
- **Z-Index hierarchy:** Regular content <50, sticky header/nav 50, side panels (Face Lock) 100, Pose Lock modal + Character modal 150, Lightbox 200, Character undo toast 250.
- **Sayfa yapısı:** (a) `/` route: Landing page (marketing/showcase, CTA butonu). (b) `/Account/Login`: Dedicated full-page login (sol hero + sağ form split layout). (c) `/RenderNet/Index` (Studio): Ana uygulama, Login sonrası landing. Route adı `/Studio`'ya rename ileride değerlendirilir. Sidebar nav: Studio / Canvas / Characters / Pricing.
- **Üretim ortamı kuyruğu (gelecek planı):** Uygulama yayına alındığında eş zamanlı istek yükünü yönetmek için **AWS SQS** ile istek kuyruğa alma sistemi eklenecek. Şu anki mimari (Controller → Orchestrator → Domain Service → fal.ai API çağrısı) tek geliştirici testleri için yeterli, ama prod'da SQS producer/consumer pattern'i geçecek. Bu yüzden:
  - Yeni iş mantığı eklerken katmanlar arası temiz sınır koru — domain service'in fal.ai API çağrı adımı ileride SQS consumer worker'ına taşınabilmeli.
  - Polling job mantığı (`GenerationPollingService`) zaten generation_id bazlı çalıştığı için SQS sonrası aynı kalabilir.
  - SQS'e karşı alternatif kuyruk sistemleri (RabbitMQ, Hangfire, Azure Service Bus) ÖNERME — karar verildi.

Üretim asenkron olduğu için arka planda **polling + SignalR** ile sonuç kullanıcıya push edilir.

Varsayılan route: `{controller=RenderNet}/{action=Index}/{id?}` — yani uygulama açıldığında `RenderNetController.Index()` çalışır (controller adı historical, rename ileride değerlendirilir).

---

## 2. Teknoloji yığını

**Backend**

- .NET 9.0 (Nullable enabled, ImplicitUsings enabled)
- ASP.NET Core MVC + SignalR (`Microsoft.AspNetCore.SignalR`)
- `System.Text.Json` (varsayılan); bazı dosyalarda `Newtonsoft.Json` import edilmiş ama aktif kullanılmıyor — **yeni kodda `System.Text.Json` kullan**
- Entity Framework Core 9.0.6 (DbContext: `AppDbContext`, SQL Server LocalDB)
- FirebaseAuthentication.net 4.1.0 (aktif, magic link akışı)
- Iyzipay 2.1.67 (aktif — Türkiye TL ödemeleri)
- Stripe.net 48.2.0 (aktif — global USD ödemeleri)

**Frontend**

- Razor Views (.cshtml)
- TailwindCSS 3.4.17 (`./css/tailwind.css` → `./css/main.css` derlenir)
- Vanilla JavaScript (modül başına bir IIFE — React/Vue YOK)
- SignalR Client (CDN üzerinden, v8.0.0)
- Font Awesome 6.4.0 (CDN)
- Firebase JS SDK (auth callback dosyaları aktif)
- `@dhiwise/component-tagger` (Tailwind build pipeline'ında)

**Dış servisler**

- **fal.ai API**: Tüm AI generation hizmetleri (image, video, character training, vb.). Base URL: `https://queue.fal.run` (async queue endpoint) ve `https://fal.run` (sync endpoint). Auth: `Authorization: Key <api-key>` header.
- **Iyzico API**: ödeme (sandbox: `https://sandbox-api.iyzipay.com`, prod: `https://api.iyzipay.com`)
- **Stripe API**: ödeme (`https://api.stripe.com`)
- **Firebase**: auth (email magic link)

---

## 3. Dizin yapısı

```
/
├── BackgroundServices/        # Arka plan servisleri (IHostedService)
│   ├── GenerationPollingService.cs   # SignalR user connection tracker (Affogato polling F.M.8'de kaldırıldı)
│   └── LoraTrainingPollingService.cs # F.M.4'te eklenir — Character LoRA training status takibi
├── Configurations/            # IOptions pattern config sınıfları
│   ├── FalAiOptions.cs               # ApiKey + BaseUrl (yeni — F.M.2'de eklenir)
│   ├── CreditPricingOptions.cs       # Tier markup multipliers (yeni — F.M.5)
│   ├── IyzicoOptions.cs              # ApiKey + SecretKey + BaseUrl
│   └── StripeOptions.cs              # SecretKey + PublishableKey + WebhookSecret
├── Controllers/               # MVC controller'ları (HTTP transport only)
│   ├── RenderNetController.cs        # Ana controller (UI hâlâ /RenderNet/Index'e bağlı, rename ileride)
│   ├── CharactersController.cs       # Character CRUD + LoRA training tetikleme
│   ├── PaymentController.cs          # Iyzico + Stripe ödeme flow
│   ├── AccountController.cs          # Firebase Auth callback'leri
│   └── HomeController.cs
├── DTOs/                      # API request/response DTO'ları
│   ├── Generation/                   # Yeni — fal.ai bazlı generation DTO'ları
│   ├── Characters/                   # CharacterDto (LoraModelUrl, Status, vb.)
│   ├── ModelCatalog/                 # Yeni — dinamik model listesi DTO'ları (F.M.5)
│   └── IyzicoPaymentDtos/            # Iyzico
├── Entities/
│   ├── Character.cs                  # LoraModelUrl, LoraTrainingStatus, LoraTrainingJobId
│   ├── ModelCatalogEntry.cs          # F.M.5'te eklenir — model whitelist + pricing cache
│   ├── AppUser.cs                    # FirebaseUid, TokenWallet bağı
│   └── ...
├── Hubs/
│   └── GenerationHub.cs              # SignalR hub — endpoint: /generationHub
├── Middlewares/
│   └── ExceptionHandlingMiddleware.cs  # Global exception handler
├── Models/
│   ├── ServiceResult.cs              # Generic ServiceResult<T> pattern
│   ├── ErrorViewModel.cs
│   └── Payment/                      # Iyzico + Stripe modelleri
├── Services/
│   ├── Interfaces/                   # Cross-cutting interfaces (IPaymentService, ICreditService vb.)
│   ├── Concretes/                    # Cross-cutting implementations
│   ├── Generation/                   # 🆕 fal.ai generation katmanı (F.M.1+)
│   │   ├── Abstractions/
│   │   │   ├── IGenerationOrchestrator.cs
│   │   │   ├── IImageGenerator.cs
│   │   │   ├── IVideoGenerator.cs
│   │   │   ├── ICharacterTrainer.cs
│   │   │   ├── IFalAiClient.cs
│   │   │   ├── IFalAiStorageClient.cs
│   │   │   └── IFalAiModelCatalog.cs
│   │   ├── Orchestrators/
│   │   │   ├── GenerationOrchestrator.cs
│   │   │   └── CharacterTrainingOrchestrator.cs
│   │   ├── Domain/
│   │   │   ├── Image/
│   │   │   │   ├── FluxDevGenerator.cs
│   │   │   │   ├── FluxSchnellGenerator.cs
│   │   │   │   └── FluxLoraGenerator.cs
│   │   │   ├── Video/                # F.M.9+ (ileride)
│   │   │   └── CharacterTraining/
│   │   │       └── FluxLoraTrainer.cs
│   │   ├── Providers/
│   │   │   └── FalAi/
│   │   │       ├── FalAiClient.cs
│   │   │       ├── FalAiStorageClient.cs
│   │   │       ├── FalAiModelCatalog.cs
│   │   │       └── Models/           # fal.ai-spesifik DTO'lar
│   │   └── Pricing/
│   │       ├── ModelTier.cs          # enum: Fast/Standard/Premium/CharacterLora/VideoFast/VideoPremium
│   │       ├── ICreditPricingService.cs
│   │       └── CreditPricingService.cs
│   └── ...
├── ViewModels/
├── Views/
│   ├── RenderNet/Index.cshtml        # Ana üretim UI'ı (Studio)
│   ├── Payment/IyzicoCheckOutForm.cshtml
│   └── Account/                      # Login, Register, vb.
├── wwwroot/
│   ├── css/                          # tailwind.css (kaynak), main.css (derlenmiş), site.css, toast.css
│   ├── js/
│   │   ├── app.js                    # 🎯 Ana orchestrator
│   │   ├── toast.js                  # Bildirim sistemi
│   │   ├── face-lock-panel.js
│   │   ├── pose-lock-panel.js
│   │   ├── character-modal.js
│   │   ├── character-panel.js
│   │   ├── model-selection-panel.js
│   │   ├── prompt-handler.js
│   │   ├── generate-button-state.js  # Generate button enable/disable logic
│   │   ├── image-controls.js
│   │   └── ...
│   └── images/, lib/
├── Migrations/                       # EF Core migration'ları
├── Properties/launchSettings.json
├── Program.cs                        # DI registration + middleware pipeline
├── SelfAI.csproj
├── appsettings.json                  # Placeholder values only (gerçek key'ler User Secrets'ta)
├── appsettings.Development.json
├── appsettings.Production.json       # .gitignore'da
├── package.json                      # Tailwind/PostCSS build scripts
└── tailwind.config.js
```

**Klasör konvansiyonları:**

- Yeni AI generation kodu **mutlaka** `Services/Generation/` altına yazılır
- DTO'lar kategori bazında alt klasörlerde (Generation, Characters, ModelCatalog, vb.)

---

## 4. fal.ai API ile entegrasyon

### 4.1 Yetkilendirme

Tüm fal.ai endpoint'leri `Authorization: Key <api-key>` header'ı gerektirir. Bu header `FalAiClient`'in HTTP client configuration'ında set edilir:

```csharp
_httpClient.BaseAddress = new Uri(_options.BaseUrl);
_httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_options.ApiKey}");
```

API key User Secrets'ta `"FalAiOptions:ApiKey"` altında. Asla appsettings.json'a veya kaynak koda yazma.

### 4.2 Generation flow'u (queue-based, async)

fal.ai async queue API'si kullanılır — request submit edilir, job ID döner, status polling yapılır. Akış:

```
[Frontend]                              [Backend]                              [fal.ai API]
    │                                       │                                       │
    │── POST /RenderNet/GenerateImage ──────▶                                       │
    │     (X-SignalR-ConnectionId)          │                                       │
    │                                       │── POST queue.fal.run/{model-id} ─────▶│
    │                                       │                                       │
    │                                       │◀── {request_id, status:IN_QUEUE} ─────│
    │◀── 200 {requestId} ───────────────────│                                       │
    │                                       │                                       │
    │                                       │   [GenerationPollingService]          │
    │                                       │   3 saniyede bir, max 60 deneme       │
    │                                       │── GET queue.fal.run/{model}/{id}/    │
    │                                       │       status ─────────────────────────▶│
    │                                       │◀── {status: IN_QUEUE | IN_PROGRESS |  │
    │                                       │     COMPLETED | FAILED} ──────────────│
    │                                       │                                       │
    │                                       │   (COMPLETED olunca result fetch)     │
    │                                       │── GET queue.fal.run/{model}/{id} ────▶│
    │                                       │◀── {images:[{url, width, height}]} ───│
    │                                       │                                       │
    │◀── SignalR: GenerationCompleted ──────│                                       │
    │     {requestId, media:[{url}]}        │                                       │
```

**Önemli noktalar:**

- Request body **her modelin kendi schema'sına göre** değişir — örn. Flux Dev `{prompt, image_size, num_inference_steps, guidance_scale}`, Veo `{prompt, duration}`, vb.
- Status değerleri (case-sensitive!): `IN_QUEUE`, `IN_PROGRESS`, `COMPLETED`, `FAILED`
- Result URL'leri fal.ai'ın CDN'inde — default 24 saat retention, long-term için S3'e kopyalama gerekir (F.M.7'de değerlendirilir)
- Webhook desteği var (gelecek optimizasyon — şimdilik polling ile başla)
- **NSFW/şiddet/kamu figürü içerik:** fal.ai content policy bunu yasaklar, hesap kapanma riski var. Negative prompt'ta zaten "nsfw, deformed..." var; pozitif prompt'ta kullanıcı bu tür içerik yazarsa backend'de filtrele (mevcut filter mantığı korunur)

### 4.3 Character LoRA training flow'u

```
1. Kullanıcı modal'da "+Yeni Karakter" → 1-5 yüz görseli yükle + name + prompt + tip
2. Backend FaceLock-style asset upload → fal.ai storage'a yükle, URL'leri al
3. Backend POST queue.fal.run/fal-ai/flux-lora-fast-training
   Input: { images_data_url: [url1, url2, ...], trigger_word: "TeomanChar" }
4. fal.ai job_id döner, Character.LoraTrainingStatus = Training, LoraTrainingJobId = job_id
5. LoraTrainingPollingService periyodik status kontrol (60s aralık, max 30 dk)
6. COMPLETED olunca:
   - Output: { diffusers_lora_file: { url: "...safetensors" } }
   - Character.LoraModelUrl = url
   - Character.LoraTrainingStatus = Ready
   - Character.TrainingCompletedAt = DateTime.UtcNow
7. Modal'da kart "READY" gösterilir, generation'da kullanılabilir
```

**Training maliyeti tahmin:** ~$2 per training (250-400s GPU). Kullanıcıya yüksek credit (örn. 500-1000 credit) yansıtılır.

### 4.4 Dynamic model catalog

`IFalAiModelCatalog` servisi şu işi yapar:

1. fal.ai'dan model listesi fetch (eğer "list all" endpoint varsa direkt, yoksa whitelist + per-model pricing query)
2. Her model için: endpoint ID, display name, thumbnail URL, category (image/video/audio/3d), provider (Black Forest Labs, Google, vb.)
3. Pricing query → her model için per-image veya per-second cost
4. `ICreditPricingService` ile tier eşle, user credit cost hesapla
5. Sonucu IMemoryCache'te 15 dk TTL ile sakla
6. Frontend `/Catalog/Image` endpoint'i çağırınca image-only filter ile döner

**Önemli — fal.ai "list all" endpoint belirsiz:** F.M.2'de araştırılır. Yoksa whitelist tabanlı yaklaşım (`ModelCatalogEntry` DB tablosu + admin paneli ile eklenip çıkarılır).

### 4.5 Asset upload

Phase 1 (mevcut): fal.ai storage
- `POST https://fal.ai/api/upload` (resmi storage endpoint) veya SDK ile `fal.storage.upload()`
- Output: `{ url, file_name, file_size }`
- 24 saat default retention
- Asset entity'de `StorageProvider = "FalAi"` + `Url` field

Phase 2 (ileride): S3 / R2 / Backblaze
- Daha bağımsız, long-term storage
- Asset entity'de `StorageProvider = "AwsS3"` veya benzeri

Yeni endpoint eklerken **mutlaka fal.ai docs'a bak**: `https://fal.ai/docs/documentation` ana index, model spesifik dokümantasyon her modelin sayfasında.

---

## 5. Mimari kurallar ve kalıplar (DİKKAT)

### 5.1 ServiceResult\<T> pattern

Servis katmanı **asla exception throw etmez**, her zaman `ServiceResult<T>` döner. Controller bunu kontrol edip uygun HTTP cevabını üretir.

```csharp
return ServiceResult<MyDto>.Success(data, "Başarılı mesaj");
return ServiceResult<MyDto>.Failure("Kullanıcıya gösterilecek güvenli mesaj", 502);
```

**Asla** API hatasının ham mesajını kullanıcıya gösterme. Detay log'a yazılır, kullanıcıya Türkçe + güvenli mesaj döner.

### 5.2 IOptions pattern

Konfigürasyon `Program.cs`'de `Configure<TOptions>` ile bağlanır, servislerde `IOptions<TOptions>` enjekte edilir. **Hardcode API key/URL yazma.** Yeni bir dış servis eklersen yeni bir `XOptions` sınıfı yarat.

### 5.3 HttpClient enjeksiyonu

Her dış servis için `AddHttpClient<TInterface, TImpl>()` ile typed HttpClient kayıtlı. Yeni dış servisler için aynı kalıbı izle — `HttpClientFactory`'yi direkt kullanma, `HttpClient`'ı `new`'leme.

### 5.4 JSON serialization

- **Yeni kodda `System.Text.Json` kullan.** Newtonsoft sadece bazı dosyalarda import edilmiş, aktif değil.
- fal.ai API yanıtları `snake_case` — DTO'larda `[JsonPropertyName("snake_case")]` attribute kullan.
- Deserialize ederken `PropertyNameCaseInsensitive = true` set et (yerleşik kalıp).
- Backend response'ları camelCase (frontend uyumlu) — `Program.cs`'de `AddJsonOptions(... CamelCase)` aktif.

### 5.5 Logging

Yapılandırılmış logging zorunlu, **pipe-separated key-value formatı** kullanılıyor:

```csharp
_logger.LogInformation(
    "Görsel oluşturma isteği başlatıldı. | Model: {Model} | ImageSize: {ImageSize} | NumImages: {NumImages}",
    modelId, imageSize, numImages);
```

Bu formatı koru. Tüm log mesajları **Türkçe**.

### 5.6 SignalR + Polling mimarisi

Generation tamamlanmasını beklemek için iki katmanlı bir sistem var:

- **`GenerationPollingService`** (Singleton): Şu an yalnızca SignalR user connection tracker (`_userConnections`). Affogato polling F.M.8'de tamamen kaldırıldı. Provider (fal.ai) polling'i artık `FalAiClient.SubmitAndWaitAsync` içinde; sonuç push'ı `GenerationOrchestrator` → SignalR `"GenerationUpdate"` event'iyle yapılır.
- **`LoraTrainingPollingService`** (F.M.4'te eklenir, Singleton + HostedService): Character LoRA training status'unu periyodik kontrol eder (60s aralık).
- **`GenerationHub`**: SignalR hub'ı. Frontend `RegisterClient(userId)` çağırır, backend connectionId ile eşleştirir.

### 5.7 Frontend modül kalıbı

Her JS dosyası IIFE pattern ile bir modül export eder:

```javascript
const ModuleName = (function () {
    'use strict';
    function init() { /* ... */ }
    return { init, /* public API */ };
})();
```

Modüller arasında bağımlılık `App.js`'teki `init*` fonksiyonlarıyla kurulur. **ES modülleri, bundler veya framework KULLANMA.**

Toast bildirimleri: `Toast.success(msg, title)`, `Toast.error(msg, title)`, `Toast.warning(...)`, `Toast.info(...)`. API çağrıları için `apiFetch(url, options)` helper'ı kullan.

### 5.8 Global exception middleware

`Program.cs`'in ilk satırında `app.UseGlobalExceptionHandling()` ile bağlı. AJAX isteklerine JSON, normal isteklere `/Home/Error` redirect döner. **Her controller'a try-catch yazmana gerek yok**. Ancak servis katmanı `ServiceResult.Failure` ile **kendi hata cevabını** dönmeli — middleware son çare.

### 5.9 Provider abstraction layer (fal.ai migration sonrası)

Generation kodu katmanlı yapıdadır:

```
Controller (HTTP transport)
    ↓ depends on
IGenerationOrchestrator (model seçimi, kredi düşme, history)
    ↓ depends on
IImageGenerator / IVideoGenerator / ICharacterTrainer (domain logic, tek model kategorisi)
    ↓ depends on
IFalAiClient (provider HTTP communication)
```

**Önemli kurallar:**

- Controller asla `FalAiClient`'a doğrudan bağlanmaz — sadece `IGenerationOrchestrator`'a bağlanır
- Domain service (örn. `FluxDevGenerator`) sadece kendi modeliyle ilgili iş mantığını bilir — başka model bilmez
- `IFalAiClient` provider-spesifik HTTP detaylarını saklar — domain service URL/header bilmez
- Yeni model eklemek = yeni Domain service class'ı (örn. `RecraftProGenerator`) implementing `IImageGenerator`. Diğer kod değişmez.
- Yeni provider eklemek (ileride) = yeni `IFalAiClient` benzeri implementation. Domain service'ler abstraction üzerinden çalışır.

### 5.10 Credit pricing servisi

`ICreditPricingService`:

```csharp
public interface ICreditPricingService
{
    decimal CalculateUserCredits(decimal falAiCostUsd, ModelTier tier);
    ModelTier MapEndpointToTier(string falAiEndpointId);
}
```

`ModelTier` enum: `Fast`, `Standard`, `Premium`, `CharacterLora`, `VideoFast`, `VideoPremium`.

Markup multipliers `CreditPricingOptions` (IOptions) ile yüklenir, business kararıyla değişebilir. Kod değişikliği gerektirmez.

---

## 6. Geliştirme komutları

### Backend

```bash
dotnet restore                    # NuGet paketlerini geri yükle
dotnet build                      # Derle
dotnet run                        # Çalıştır (https://localhost:7117, http://localhost:5175)
dotnet run --launch-profile https # HTTPS profilinde
dotnet ef migrations add <Name>   # Yeni EF migration
dotnet ef database update         # Migration uygula
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

# fal.ai
dotnet user-secrets set "FalAiOptions:ApiKey" "fal-key-..."
dotnet user-secrets set "FalAiOptions:BaseUrl" "https://queue.fal.run"

# Iyzico
dotnet user-secrets set "IyzicoOptions:ApiKey" "sandbox-..."
dotnet user-secrets set "IyzicoOptions:SecretKey" "sandbox-..."
dotnet user-secrets set "IyzicoOptions:BaseUrl" "https://sandbox-api.iyzipay.com"

# Stripe
dotnet user-secrets set "StripeOptions:SecretKey" "sk_test_..."
dotnet user-secrets set "StripeOptions:PublishableKey" "pk_test_..."
dotnet user-secrets set "StripeOptions:WebhookSecret" "whsec_..."

# Firebase
dotnet user-secrets set "Firebase:ServiceAccountJson" "<JSON inline veya base64>"

# Google OAuth
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."

# SMTP
dotnet user-secrets set "Smtp:Password" "..."
```

**Listeleme/silme:**

```bash
dotnet user-secrets list          # Tüm secret'ları listele
dotnet user-secrets remove <key>  # Belirli bir key'i sil
dotnet user-secrets clear         # Hepsini sil
```

---

## 7. Bilinen eksiklikler / TODO

### Migration TODO (öncelikli)

- **F.M.1 Foundation refactor** — `Services/Generation/*` klasör yapısı + interface skeleton + DI registration. Affogato kodu legacy klasöre taşıma. **Functional değişiklik YOK.**
- **F.M.2 IFalAiClient HTTP setup** — Auth, queue endpoint, polling, webhook altyapısı.
- **F.M.3 Image generation** — Flux Dev/Schnell ile basit prompt→image akışı, Studio'da test.
- **F.M.4 Character LoRA training** — Eski Affogato karakter sistemi devre dışı, yeni LoRA-based sistem entegre.
- **F.M.5 Multi-model UI** — Dinamik catalog, Studio'da kart liste render.
- **F.M.6 Pose Lock + Face Lock** — IP-Adapter / ControlNet ile yeniden bağla.
- **F.M.7 Asset upload** — fal.ai storage entegrasyonu.
- **F.M.8 Affogato cleanup** — Legacy klasörünü tamamen sil, RenderNetOptions config'ini sil.

### Ana özellik TODO (gelecek)

- **F.M.9 Video generation** — fal.ai Veo, Kling, Seedance entegrasyonu.
- **F.M.10 Product/clothes try-on** — fal.ai idm-vton, ace-step entegrasyonu.
- **F.M.11 Social media presets** — TikTok 9:16, Insta 1:1/4:5 aspect ratio + style preset wrapper'ları.
- **F.7 Landing redesign** — Affogato-style value prop, target audience clarity, social media use cases.
- **F.9 Admin paneli** — Eysstalent benzeri user/credit/generation yönetimi + fal.ai havuz kapasitesi izleme.
- **F.10 Landing FAQ accordion** — Açılır/kapanır Q&A bölümü.

### Production hazırlık

- AWS SQS entegrasyonu (queueing)
- Environment config (dev/prod ayrımı)
- API key vault (User Secrets → Azure KeyVault veya env vars)
- Error monitoring (Sentry vb.)
- Domain + SSL
- CI/CD pipeline
- Firebase Auth magic link custom SMTP (mevcut `noreply@selfai-df09f.firebaseapp.com` spam'e düşüyor)

---

## 8. KESİN YAPILMAMASI gerekenler

1. **API key'leri asla** appsettings.*.json, kaynak kod veya commit mesajına yazma. User Secrets veya env variable kullan.
2. **Yeni kod hiçbir şekilde Affogato API'sine endpoint çağrısı yapmaz.** Eski `IRenderNetApiClient` kodu legacy klasörde sadece referans olarak duruyor, F.M.8'de silinecek. Yeni iş `IFalAiClient` üzerinden gider.
3. **Hardcoded model listesi YAZMA.** Studio'da gösterilen modeller `IFalAiModelCatalog` üzerinden dinamik gelir. Frontend asla "Flux Dev" gibi sabit isimleri kart olarak görmez.
4. **Hardcoded credit cost YAZMA.** Tüm credit hesaplamaları `ICreditPricingService.CalculateUserCredits()` üzerinden gider. Markup multiplier'ları config'den okunur.
5. **Provider lock-in oluşturma.** Domain service'ler (`FluxDevGenerator`, `FluxLoraTrainer` vb.) `IFalAiClient` interface'ine bağlanır, `FalAiClient` concrete'ine değil. İleride başka provider eklemek için bu disiplin kritik.
6. **`GenerationPollingService` artık yalnızca SignalR user connection tracker'dır** (`_userConnections`). Affogato polling (`_activeJobs`/`_pendingResults`) F.M.8'de kaldırıldı; provider polling `FalAiClient.SubmitAndWaitAsync` içinde, sonuç push'ı `GenerationOrchestrator` → SignalR `"GenerationUpdate"`.
7. **`Program.cs`'deki HttpClient registration sırasını bozma** — typed client'lar her servis için ayrı kayıtlı, paylaşılmamalı.
8. **SignalR hub endpoint'ini değiştirme** — `/generationHub`. Frontend buna bağlı.
9. **Frontend'i framework'leştirme** (React/Vue/Svelte) — IIFE modül kalıbı kasıtlı.
10. **Türkçe log/comment'leri İngilizce'ye çevirme** — proje dili Türkçe, tutarlılığı koru.
11. **`ServiceResult` pattern'ini bypass etme** — controller'a exception throw eden servis yazma.
12. **Newtonsoft.Json ekleme** — `System.Text.Json` kullan. Mevcut Newtonsoft import'ları temizlenebilir.
13. **NSFW/şiddet/kamu figürü içerik üreten kod ekleme** — fal.ai content policy bunu yakalarsa hesap kalıcı kapanır.
14. **`AspNetCore.Identity` ekleme** — Firebase Auth planı aktif, ikisini birden kullanma.
15. **MVC katman ihlali** — Controller'da iş mantığı, Service'te HttpContext erişimi, DTO'da method — bunlardan herhangi biri YASAK.
16. **Markup tier sistemini bypass etme** — yeni model eklendiğinde mutlaka bir `ModelTier`'a map'lenir, doğrudan dolar→kredi yapılmaz.

---

## 9. Çalışma tarzı

- **Dil:** Tüm cevaplar, log mesajları, comment'ler **Türkçe**.
- **Rol dağılımı:** Ben (kullanıcı) komut/spec veririm, sen (Claude) kodu yazarsın. Ben kontrol eder ve kritik yerlerde devreye girerim. Pair programming tarzı.
- **Ama körü körüne yazma:** Bir özellik veya değişiklik istendiğinde, yazmadan önce şunları kısaca açıkla:
  1. **Hangi dosyaları değiştireceksin / oluşturacaksın** (liste halinde).
  2. **Mevcut hangi kalıba uyacaksın** (ServiceResult, IOptions, typed HttpClient, IIFE modül, IGenerationOrchestrator katmanlaması vs.).
  3. **API dokümantasyonu gerekli mi**, gerekiyorsa hangi endpoint'e bakacaksın (fal.ai docs: `https://fal.ai/docs/documentation`).
  4. Belirsizlik varsa **soruyla netleştir**, varsayım yapma.
  Bu mini-plan onaylandıktan sonra kodu yaz.
- **Küçük/açık değişiklikler için** (tek satırlık fix, typo, log mesajı düzeltme vs.) mini-plan atlanabilir — direkt yaz.
- **Büyük refactor veya mimari değişiklik** öneriyorsan: önce **mevcut kalıba neden uymadığını** açıkla, alternatif öner, onay iste.
- **Bilmediğin API davranışı varsa uydurma** — fal.ai docs'a bak. Model spesifik input/output schema her modelin kendi sayfasında.
- **Test/doğrulama:** Yeni endpoint veya servis eklediğinde, kullanıcının nasıl test edebileceğini (örnek payload, curl komutu, UI adımı) söyle. fal.ai'da gerçek çağrı credit harcayacağı için test sırasında Flux Schnell + 512×512 tercih et (en ucuz kombinasyon).
- **Yarım iş bırakma:** Bir dosyayı düzenlerken `// TODO`, `// FIXME`, `throw new NotImplementedException()` ekleyeceksen önce sor — gerçekten gerekli mi, yoksa o anda tamamlanabilir mi?
- **Kritik dosyalara dokunmadan önce uyar:** `Program.cs`, `GenerationPollingService.cs`, `GenerationHub.cs`, `ExceptionHandlingMiddleware.cs`, `IFalAiClient.cs` (F.M.2 sonrası), `IGenerationOrchestrator.cs` — bunlardan birinde değişiklik gerekiyorsa "şu dosyaya şu nedenle dokunacağım, onaylıyor musun?" diye sor.
- **Migration phase disiplini:** Yeni özellik talep edildiğinde, hangi phase'in (F.M.X) parçası olduğunu netleştir. Phase atlamaya çalışma — örn. F.M.4 (Character LoRA) yapılmadan F.M.5'e (Multi-model UI) geçme.

---

## TECH DEBT

**MARS warning in payment flows** — "Savepoints disabled because MARS enabled." Manual rollback handling expected. Consider refactor to disable MARS or restructure DB context if transactional safety becomes a concern. Not critical now due to existing idempotency + DB lock.

**Stripe.net + Iyzipay paralel kullanım** — İki ödeme sağlayıcı paralel aktif. Webhook/callback handling'i her ikisinde de idempotent. Tek bir başarısız ödeme rollback'i için ikisinde de unit test ileride yazılmalı.

**fal.ai cost monitoring** — Admin paneli (F.9) yapılana kadar fal.ai havuz kapasitesi manuel izleniyor. Aşırı satım riski (kullanıcılara dağıtılan toplam credit > fal.ai havuzu) admin'in sorumluluğunda.