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
- **Landing redirect (F.7.2):** `HomeController.Index` authenticated kullanıcıyı `RenderNetController.Index`'e redirect eder, anonim kullanıcı `Views/Home/Index` (landing MVP) görür.
- **Landing layout (F.7.2):** Landing sayfası `_LandingLayout.cshtml` kullanır (dark theme, minimal nav, sticky header). Studio için ayrı `_AppLayout.cshtml` korunur, dokunulmaz.
- **SEO (F.7.2):** `robots.txt` + `sitemap.xml` `wwwroot/` altında; meta tag'ler `_LandingLayout.cshtml` içinde (description, keywords, OG, Twitter Card, canonical URL).
- **Analytics (F.7.2):** GA4 conditional script yalnızca `_LandingLayout`'ta yüklenir (`Analytics:MeasurementId` dolu ise). Studio (`_AppLayout`) tracking'siz. Kullanıcı davranışı takibi F.9 admin panelinde ele alınır.
- **Admin yetkilendirme (F.7.3):** ASP.NET Core Identity Role (`'Admin'`), `AdminOptions.AllowedEmails` whitelist üzerinden `AdminSeedHostedService` ile startup'ta atanır. Firebase auth'tan bağımsız Identity-side rol sistemi.
- **Admin operasyon audit (F.7.3):** `TokenTransaction.AdminUserId` + `AdminNote` column'ları admin işlemlerini izler. `Type='AdminGrant'` filter'ı ile admin işlemleri ayrıştırılır.
- **F.7.3 mini-admin scope:** sadece FindUserByEmail + AddCredit. Tam admin paneli (kullanıcı listesi paginated, transaction history UI, kredi çıkarma, ödeme yönetimi, analytics dashboard) F.9'a ertelendi.
- **AddCredit validation (F.7.3):** miktar 1-10000, not max 500 karakter, DB transaction ile atomik (wallet update + transaction insert aynı transaction'da).
- **Studio bilgi mimarisi (F.M.Arch.1):** `/Studio` hub (`StudioHubController`) + `/Studio/{Image,Templates,Music,Video}` sekmeler. Her sekme ayrı controller (`Controllers/Studio/` klasörü), ayrı view (`Views/Studio/`), paylaşılan servisler (credit, asset, generation orchestrator, `IFalAiClient`).
- **/RenderNet redirect-only backward compat (F.M.Arch.1):** `/RenderNet/Index` → `/Studio/Image`. `RenderNetController` class korundu, action gövdesi sadece `RedirectToAction("Index", "ImageStudio")`. F.M.9+'da tam kaldırılabilir.
- **View çözümleme pattern (F.M.Arch.1):** `Controllers/Studio/*Controller`'lar ortak `Views/Studio/` klasörünü kullanır. Convention `Views/{ControllerName}/`'ı arar → uyumsuz. Her action explicit path verir: `return View("~/Views/Studio/{ViewName}.cshtml")`.
- **Studio Index anonim erişim (F.M.Arch.1):** mevcut `RenderNetController` davranışı korundu, `[AllowAnonymous]` attribute Studio ana ekranında (`Image.cshtml`) aktif. Login zorunlu değil — kullanıcı sonuçları alamaz ama arayüzü görebilir.
- **Nav active highlight (F.M.Arch.1):** tüm `Studio*` controller'ları kapsar: `StudioHub`, `ImageStudio`, `TemplatesStudio`, `MusicStudio`, `VideoStudio`.
- **Templates format catalog (F.M.10a):** statik 6 format (Instagram Post, Story, TikTok/Reels Cover, YouTube Thumbnail, YouTube Shorts, Facebook/LinkedIn Post) kod içinde `TemplateCatalogService`'de tanımlı. F.9 admin'de CRUD gelene kadar hardcoded.
- **Templates model mapping (F.M.10a):** 1:1 + 16:9 → Ideogram V3 (typography kritik), 9:16 → Nano Banana (hız + fiyat). F.9'da mapping esnek yapılabilir.
- **Templates üretim mimarisi (F.M.10a):** `TemplatesStudioController.Generate` doğrudan `IGenerationOrchestrator.StartGenerationAsync`'i çağırır. Ayrı `TemplateGenerationOrchestrator` YOK — kredi/iade/log/SignalR mantığı `GenerationOrchestrator`'da tekil, DRY. Format'a göre `StartGenerationRequest` build eden lightweight static builder (`TemplateStartRequestBuilder`) kullanılır.
- **Templates aspect ratio (F.M.10a):** mevcut `GenerationOrchestrator.MapAspectRatioToImageSize` kullanılır (9:16 → `portrait_16_9`). **Ideogram V3** `image_size` enum'unu kabul eder (fal.ai OpenAPI doğrulandı) → onun için hiçbir değişiklik yok. **Nano Banana yalnızca `aspect_ratio` kabul eder, `image_size`'ı yok sayar** (fal.ai OpenAPI doğrulandı); bu yüzden `DynamicImageGenerator`'a aspect_ratio-native model converter'ı eklendi (`image_size`→`aspect_ratio`, whitelist'te sadece `fal-ai/nano-banana`). image_size-native modeller (ideogram/flux/recraft) ETKİLENMEZ. Yan etki: Studio/Image'de nano-banana artık aspect seçimine uyar (önceden hep 1:1 — latent bug fix, onaylandı).
- **Templates transparent prompt (F.M.10a):** kullanıcı format seçince kompozisyon suffix'i görünür ve isterse checkbox ile kaldırabilir. Örn: `artisan coffee cup` + `, centered composition, clean background...` suffix. Suffix birleştirme `TemplateStartRequestBuilder`'da yapılır.
- **Templates SignalR (F.M.10a):** `template-studio.js` kendi SignalR bağlantısını kurar (app.js bu sayfada yüklenmez), başlattığı generationId'leri Map'te tutar, `GenerationUpdate` event'ini paylaşır ama yalnızca kendi ID'lerini işler — Studio/Image tarafını etkilemez. Her sekme sadece kendi ID'lerini işler.
- **F.9a admin panel (kilitli):** Dashboard (4 metric card — bugün/bu hafta user, bugün/bu hafta generation, bugün/bu hafta credit tüketimi, toplam user + bu hafta admin grant toplamı), paginated user list (email/isim arama + son 7/30/tümü tarih filtresi), user detail sayfası (bilgi + paginated transaction history, tek sayfa). `/Admin` ana sayfası artık Dashboard'a gider. Kredi ekleme F.7.3'ten (SearchUser + AddCredit) aynen korunur; F.7.3 tekil arama, paginated listenin arama kutusuna entegre edildi (POST SearchUser → `Users?searchTerm=`'e redirect).
- **F.9a batch lookup pattern (kilitli):** user list'te wallet (bakiye), generation sayısı ve kredi tüketimi istatistikleri için N+1 önlenir — her kaynak tipi için tek batch query + dictionary lookup (`userIds.Contains(...)` + `GroupBy`). Tüm read-only query'lerde `AsNoTracking()`. Toplam sorgu sayısı user sayısından bağımsız sabittir.
- **F.9a admin durumu — Identity YOK (kilitli):** Proje ASP.NET Identity/Role sistemi kullanmaz; `IsAdmin` bilgisi `AdminOptions.AllowedEmails`'ten hesaplanır (normalize edilmiş email HashSet ile batch kontrol, `UserManager`/`RoleManager` yok). `_db.Roles`/`_db.UserRoles` tabloları mevcut değildir.
- **F.9a soft-delete durumu (kilitli):** `AppUser`'da `DeletedAt` (soft-delete) alanı **YOK** → sayım/liste query'lerinde soft-delete filtresi uygulanmaz, `AdminUserDetailDto.IsDeleted` her zaman `false` döner. Soft-delete F.9b'de gelirse filtreler ve `IsDeleted` o zaman doldurulur. (Generation sayımı `Generations` tablosundan, kredi tüketimi negatif `TokenTransaction` toplamından; `TokenTransaction`'da `BalanceAfter` alanı olmadığı için işlem geçmişinde "sonraki bakiye" gösterilmez.)
- **F.9a scope dışı (F.9b'ye ertelendi):** model catalog UI, sistem ayarları, kredi çıkarma (negatif işlem), rol yönetimi UI, ödeme (Iyzico/Stripe) transaction takibi, kullanıcı ban/unban.
- **F.M.10b Post Templates gelişmiş (kilitli):** 3 bileşen aktif — text overlay (server-side ImageSharp v2.1), 5 preset templates (statik katalog), Character LoRA + Templates entegrasyonu.
- **F.M.10b text overlay stack (kilitli):** SixLabors.ImageSharp v2.1.x + Drawing beta15 + Fonts v1.0.x (Apache 2.0, ücretsiz). Inter font family Türkçe karakter destekli. `wwwroot/fonts/` altında Regular/Bold/SemiBold. (Sürüm pinleme uyarısı için bkz. BUGFIX HISTORY: Fonts beta18 sabit.)
- **F.M.10b preset templates (kilitli):** statik `PresetTemplateCatalogService` (F.9b'de admin CRUD gelecek). Her preset: layout config + text field'lar (sabit koordinat) + prompt suffix + model endpoint + aspect ratio.
- **F.M.10b Character LoRA + Templates (kilitli):** `StartGenerationRequest.CharacterId` propagate edilir, orchestrator zaten endpoint override + LoRA URL resolve + Ready/ownership validation yapıyor. Controller'da tekrar validation YOK (DRY).
- **F.M.10b preset post-processing (kilitli):** `IMemoryCache` pending data (1 saat TTL, generationId keyed). SignalR `GenerationUpdate` Complete geldiğinde frontend PostProcess endpoint'ini çağırır, backend text overlay render eder R2'a upload eder.
- **F.M.10b request builder ayrımı (kilitli):** `PresetStartRequestBuilder.Build(preset, dto)` — endpoint override parametresi YOK, orchestrator'a bırakılır. Aynı prensip `TemplateStartRequestBuilder.Build(format, dto)`'a `CharacterId` propagate edilir.
- **F.M.UI.2 kredi bakiyesi ViewComponent (kilitli):** `CreditBalanceViewComponent` (`ViewComponents/`) + `Views/Shared/Components/CreditBalance/Default.cshtml`. `_AppLayout.cshtml` VE `_AdminLayout.cshtml` içinde `@await Component.InvokeAsync("CreditBalance")` — tüm app sayfalarında tek kaynaktan, **server-side** bakiye (ilk boyamada doğru rakam, "—" flash'ı yok). Kimlik `HttpContext.User`'ın `AppUserId` claim'inden çözülür (proje genelinde `IUserContextService` YOK; `AccountController.Balance` de aynı claim'i kullanır), `ICreditService.GetBalanceAsync(Guid)` çağrılır — **CancellationToken parametresi YOK**. Fail-gracefully: bakiye çekilemezse `Balance=null` → view `—` gösterir. Mevcut `.app-nav__credit-pill` markup + `#creditBalance`/`#creditBalanceValue` id'leri KORUNDU; **yeni `credit-balance.css` YARATILMADI** (pill stili `app-nav.css`'te; `_AdminLayout` bu css'i yüklemediği için ona `app-nav.css` link'i eklendi). Admin pill'i admin'in KENDİ bakiyesini gösterir (target kullanıcının değil).
- **F.M.UI.2 bakiye güncelleme mekanizması (kilitli):** ⚠️ Bakiye için SignalR `CreditUpdated` event'i **YOKTUR** (spec taslağındaki varsayım yanlıştı). ViewComponent değeri server-side render eder; canlı güncelleme yalnızca generation sayfalarında `fetch('/Account/Balance')` → `#creditBalanceValue.textContent` ile yapılır — 3 dosyada tekrarlanır: `app.js` (Studio/Image), `template-studio.js`, `preset-studio.js`. Studio Hub/Admin gibi üretim yapılmayan sayfalarda canlı güncelleme yok, server-side değer statik kalır (kabul edilir — o sayfalarda bakiye değişmez). Yeni sayfada bakiye güncelleyeceksen `#creditBalanceValue` id'sini + `/Account/Balance` endpoint'ini kullan.
- **F.M.UI.2 mobile responsive standardı (kilitli):** Studio/Image için 720px breakpoint. Layout Tailwind utility class'larıyla kurulur (spec'teki `.studio-layout`/`.studio-sidebar` YOK): container `.studio-page` (`flex h-screen`), ana canvas `#mainCanvas` (`order-1`), sağ ayar paneli `#rightSidebar` (`order-3`). Mobile kuralları `generated-results.css`'te (`main.css` Tailwind build çıktısı — elle düzenlenmez; id seçiciler utility class'ları yener): `.studio-page`→`flex-direction:column`, `#rightSidebar` alta taşınır (`order:2`, `width:100%`, `height:auto`), `#mainCanvas` `min-height:55vh`, sonuç grid'i (`.generated-results__grid`) 2 sütun. Nav mobil (hamburger, 767px) zaten `app-nav.css`'te — dokunulmadı. Sekme/bottom-sheet YOK.
- **F.M.UI.2 regenerate mimarisi (kilitli):** Frontend re-submit pattern, backend'de SIFIR değişiklik. Sonuç kartındaki (`.generated-card`, `image-controls.js`) hover Regenerate ikonu mevcut `GenerateImage` endpoint'ini yeniden çağırır. Üretim metadata'sı (modelEndpoint/prompt/aspectRatio/characterId/characterMode/faceAssetId/faceWeight) kartta **JS closure**'da tutulur — **`data-attribute` DEĞİL** (Türkçe/tırnak içeren prompt'ta HTML-escape sorununu önler; spec taslağı data-attribute diyordu, closure tercih edildi). Seed gönderilmez (backend rastgele üretir), `numImages=1`'e sabitlenir. Normal submit ve regenerate tek `submitGeneration(payload)` yolunu paylaşır (DRY); `isGenerating` flag'i paralel üretim + kredi race'ini önler. Sadece Studio/Image'da — Templates kendi flow'unu kullanır (ayrı render, regenerate butonu yok).
- **F.M.UI.2 pendingRequests Map (kilitli):** `app.js`'te `Map<generationId, StartGenerationRequest payload>`. `submitGeneration` başarılı POST sonrası payload'ı saklar; SignalR `GenerationUpdate` (Completed) geldiğinde `pendingRequests.get(generationId)` ile çekilip `ImageControls.showGeneratedImages(urls, request)`'e geçirilir (kartın **closure**'ına bağlanır, data-attribute'a yazılmaz), sonra `delete` edilir. Session-based: sayfa refresh'inde Map boşalır → eski kartlar regenerate EDİLEMEZ (kabul edilen kısıt; persistent regenerate için generation history endpoint'i F.9b'de eklenebilir).
- **Üretim ortamı kuyruğu (gelecek planı):** Uygulama yayına alındığında eş zamanlı istek yükünü yönetmek için **AWS SQS** ile istek kuyruğa alma sistemi eklenecek. Şu anki mimari (Controller → Orchestrator → Domain Service → fal.ai API çağrısı) tek geliştirici testleri için yeterli, ama prod'da SQS producer/consumer pattern'i geçecek. Bu yüzden:
  - Yeni iş mantığı eklerken katmanlar arası temiz sınır koru — domain service'in fal.ai API çağrı adımı ileride SQS consumer worker'ına taşınabilmeli.
  - Polling job mantığı (`GenerationPollingService`) zaten generation_id bazlı çalıştığı için SQS sonrası aynı kalabilir.
  - SQS'e karşı alternatif kuyruk sistemleri (RabbitMQ, Hangfire, Azure Service Bus) ÖNERME — karar verildi.

Üretim asenkron olduğu için arka planda **polling + SignalR** ile sonuç kullanıcıya push edilir.

Varsayılan route: `{controller=Home}/{action=Index}` — `HomeController.Index` authenticated kullanıcıyı Studio'ya yönlendirir, anonim kullanıcıya landing gösterir. Eski `{controller=RenderNet}` default route'u devre dışı (Program.cs'de comment'li, F.M.Arch.1). `RenderNetController` artık redirect-only backward compat kabuğu (`/RenderNet/Index` → `/Studio/Image`).

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
│   ├── StripeOptions.cs              # SecretKey + PublishableKey + WebhookSecret
│   └── AdminOptions.cs               # F.7.3 — AllowedEmails whitelist (Admin rol atama)
├── Controllers/               # MVC controller'ları (HTTP transport only)
│   ├── Studio/                       # 🆕 F.M.Arch.1 — Studio sekme controller'ları (ortak Views/Studio/)
│   │   ├── StudioHubController.cs        # /Studio hub (sekme seçim ekranı)
│   │   ├── ImageStudioController.cs      # /Studio/Image (mevcut RenderNet davranışı taşındı)
│   │   ├── TemplatesStudioController.cs  # /Studio/Templates (F.M.10b GÜNCELLENDİ — Generate 2 akış + PostProcess yeni action)
│   │   ├── MusicStudioController.cs      # /Studio/Music (placeholder, F.M.10c)
│   │   └── VideoStudioController.cs      # /Studio/Video (placeholder, F.M.9)
│   ├── RenderNetController.cs        # F.M.Arch.1 — redirect-only backward compat (/RenderNet/Index → /Studio/Image)
│   ├── CharactersController.cs       # Character CRUD + LoRA training tetikleme
│   ├── PaymentController.cs          # Iyzico + Stripe ödeme flow
│   ├── AccountController.cs          # Firebase Auth callback'leri
│   ├── AdminController.cs            # F.7.3 — mini-admin (FindUserByEmail + AddCredit)
│   └── HomeController.cs
├── DTOs/                      # API request/response DTO'ları
│   ├── Generation/                   # Yeni — fal.ai bazlı generation DTO'ları
│   ├── Templates/                    # F.M.10a — PostFormatDto, TemplateGenerationRequest
│   │                                 # F.M.10b — TextOverlaySpec, PresetTemplateDto,
│   │                                 #           PresetTextFieldDto, PresetPostProcessRequest
│   ├── Characters/                   # CharacterDto (LoraModelUrl, Status, vb.)
│   ├── ModelCatalog/                 # Yeni — dinamik model listesi DTO'ları (F.M.5)
│   ├── Admin/                        # F.7.3 — AdminUserDto, AdminGrantResultDto
│   │                                 # F.9a — AdminUserListDto, AdminUserListItemDto, AdminUserDetailDto,
│   │                                 #        AdminTransactionDto, AdminTransactionListDto, AdminDashboardStatsDto
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
│   │   ├── IAdminService.cs          # F.7.3 — mini-admin servis kontratı
│   │   ├── ITemplateCatalogService.cs # F.M.10a — statik 6 format kataloğu kontratı
│   │   ├── ITextOverlayService.cs    # F.M.10b — server-side text overlay render kontratı
│   │   └── IPresetTemplateCatalogService.cs # F.M.10b — statik 5 preset kataloğu kontratı
│   ├── Concretes/                    # Cross-cutting implementations
│   │   ├── AdminService.cs           # F.7.3 — FindUserByEmail + AddCredit iş mantığı
│   │   ├── AdminSeedHostedService.cs # F.7.3 — startup'ta AllowedEmails'e Admin rolü atar
│   │   ├── TemplateCatalogService.cs # F.M.10a — 6 sabit sosyal medya formatı (hardcoded)
│   │   ├── TextOverlayService.cs     # F.M.10b — ImageSharp v2.1 ile görsele metin render
│   │   └── PresetTemplateCatalogService.cs # F.M.10b — 5 sabit preset (hardcoded, F.9b'de CRUD)
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
│   │   ├── Builders/                 # F.M.10b — request builder'lar (Orchestrators'tan ayrıştı)
│   │   │   ├── TemplateStartRequestBuilder.cs  # F.M.10a→10b GÜNCELLENDİ — format → StartGenerationRequest, CharacterId propagate
│   │   │   └── PresetStartRequestBuilder.cs    # F.M.10b YENİ — preset → StartGenerationRequest (endpoint override YOK)
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
│   ├── Admin/                        # F.7.3 — AdminUsersViewModel, AddCreditViewModel
│   │                                 # F.9a — AdminUsersListViewModel, AdminUserDetailViewModel, AdminDashboardViewModel
│   └── Templates/                    # F.M.10a — TemplatesIndexViewModel + PostFormatViewModel
│                                     # F.M.10b — PresetTemplateViewModel, PresetTextFieldViewModel,
│                                     #           UserCharacterViewModel
├── ViewComponents/                   # 🆕 F.M.UI.2 — MVC ViewComponent'ler
│   └── CreditBalanceViewComponent.cs # Kredi bakiyesi pill (AppUserId claim + GetBalanceAsync, fail-gracefully)
├── Views/
│   ├── Studio/                       # 🆕 F.M.Arch.1 — ortak view klasörü (Studio/* controller'ları explicit path ile çözer)
│   │   ├── Hub.cshtml                    # /Studio hub sekme seçim ekranı
│   │   ├── Image.cshtml                  # Ana üretim UI'ı (eski RenderNet/Index içeriği taşındı)
│   │   ├── Templates.cshtml              # F.M.10a→10b GÜNCELLENDİ — tab layout (format + preset + character)
│   │   ├── Music.cshtml                  # placeholder (F.M.10c)
│   │   └── Video.cshtml                  # placeholder (F.M.9)
│   ├── Payment/IyzicoCheckOutForm.cshtml
│   ├── Admin/                        # F.7.3 — mini-admin sayfaları (+ F.9a genişletme)
│   │   ├── Dashboard.cshtml          # F.9a YENİ — 4 metric card özet
│   │   ├── Users.cshtml              # F.9a GÜNCELLENDİ — paginated tablo + arama/tarih filtresi
│   │   ├── UserDetail.cshtml         # F.9a YENİ — kullanıcı bilgi + paginated transaction history
│   │   └── AddCredit.cshtml          # Kredi ekleme formu (F.7.3, dokunulmadı)
│   ├── Shared/
│   │   ├── _LandingLayout.cshtml     # F.7.2 — Landing layout (dark, minimal nav, sticky header, SEO meta + GA4)
│   │   ├── _AppLayout.cshtml         # F.M.UI.2 GÜNCELLENDİ — bakiye pill artık CreditBalance ViewComponent invoke
│   │   ├── _AdminLayout.cshtml       # F.7.3 (+ F.M.UI.2 GÜNCELLENDİ — app-nav.css link + CreditBalance invoke)
│   │   └── Components/
│   │       └── CreditBalance/
│   │           └── Default.cshtml    # 🆕 F.M.UI.2 — .app-nav__credit-pill markup (server-side değer, "—" fallback)
│   └── Account/                      # Login, Register, vb.
├── wwwroot/
│   ├── css/                          # tailwind.css (kaynak), main.css (derlenmiş), site.css, toast.css
│   │   ├── landing.css               # F.7.2 — Landing sayfası stilleri
│   │   ├── admin.css                 # F.7.3 — Admin panel stilleri
│   │   ├── studio-hub.css            # F.M.Arch.1 — Studio hub + sekme nav stilleri
│   │   ├── template-studio.css       # F.M.10a→10b GÜNCELLENDİ — tab, character selector, preset stilleri
│   │   ├── app-nav.css               # Üst nav + .app-nav__credit-pill bakiye stili (F.M.UI.2 bakiye buradan; yeni credit-balance.css YOK)
│   │   └── generated-results.css     # F.M.UI.1b (+ F.M.UI.2 GÜNCELLENDİ — regenerate buton stili + Studio/Image 720px mobile layout)
│   ├── js/
│   │   ├── app.js                    # 🎯 Ana orchestrator (Studio/Image; F.M.UI.2 GÜNCELLENDİ — pendingRequests Map, submitGeneration, regenerate, isGenerating mutex)
│   │   ├── template-studio.js        # F.M.10a — Templates (kendi SignalR bağlantısı + pending kart)
│   │   ├── preset-studio.js          # F.M.10b YENİ — preset akışı (SignalR Complete → PostProcess çağrısı)
│   │   ├── toast.js                  # Bildirim sistemi
│   │   ├── face-lock-panel.js
│   │   ├── pose-lock-panel.js
│   │   ├── character-modal.js
│   │   ├── character-panel.js
│   │   ├── model-selection-panel.js
│   │   ├── prompt-handler.js
│   │   ├── generate-button-state.js  # Generate button enable/disable logic
│   │   ├── image-controls.js         # F.M.UI.2 GÜNCELLENDİ — sonuç kartı regenerate butonu (metadata closure'da, data-attribute DEĞİL)
│   │   ├── landing.js                # F.7.2 — Landing etkileşimleri (IIFE)
│   │   └── ...
│   ├── fonts/                        # F.M.10b — Inter-Regular.ttf, Inter-Bold.ttf, Inter-SemiBold.ttf (text overlay)
│   ├── img/
│   │   ├── landing/                  # F.7.2 — 7 görsel: hero-1/2/3.jpg, feature-character.jpg,
│   │   │                             #   feature-face-lock.jpg, feature-multi-model.jpg, og-image.jpg
│   │   │                             #   (deploy öncesi Studio'da üretilecek)
│   │   └── presets/                  # F.M.10b — 5 preview mockup (preset kart önizleme)
│   ├── robots.txt                    # F.7.2 — SEO
│   ├── sitemap.xml                   # F.7.2 — SEO
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

**Options SectionName standardizasyonu (F.7.2):** Her Options sınıfında `public const string SectionName` tanımlı olmalı; `Program.cs` bind'i bu const'ı kullanır (magic string yok). F.7.2'de mevcut isimler korundu: `FalAiOptions='FalAiOptions'`, `IyzicoOptions='IyzicoOptions'`, `CreditPricingOptions='CreditPricing'`, `R2Options='R2'`. Rename (kısa isme çevirme) F.9 admin refactor'unda değerlendirilecek.

**Firebase istisnası (F.7.2):** Firebase için Options sınıfı YOK, `Program.cs` raw okuma yapıyor (`builder.Configuration["Firebase:CredentialsPath"]`). F.9 admin phase'ine kadar bu pattern korunur.

**Admin konfigürasyonu (F.7.3):** `AdminOptions.AllowedEmails` (`List<string>`) User Secrets'ta indexed key ile (`Admin:AllowedEmails:0`). Production env variable: `Admin__AllowedEmails__0`. Bootstrap süreci: 1) email User Secrets'a eklenir, 2) hedef kullanıcı Firebase login yapar, 3) app restart → `AdminSeedHostedService` rolü otomatik atar.

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

**Rename migration notu (F.7.2):** User Secrets key isimleri Options `SectionName` ile eşleşir. Şu an kullanılan: `'FalAiOptions:ApiKey'`, `'IyzicoOptions:ApiKey'`, `'IyzicoOptions:SecretKey'`. İleride kısa isme rename olursa (`'FalAi:*'`, `'Iyzico:*'`), User Secrets + `launchSettings.json` environment variable'ları da güncellenmeli.

---

## 7. Bilinen eksiklikler / TODO

### Durum (F.7.3 sonrası)

- **F.7 tamamlandı.** F.7 = Landing + Production Prep (R2, environment separation, SEO, Analytics, mini-admin). Beta launch teknik olarak hazır. Deploy adımları: landing görselleri + SmarterASP env variable setup + Firebase JSON upload.
- **Sıradaki:** 1) Landing görselleri (Studio'da Character LoRA + Face Lock ile üretim, ~1 saat), 2) SmarterASP production deploy (~2-3 saat). Beta launch invite-only, 10-20 kişi hedeflendi.
- **F.M.9+** (video generation, sosyal medya post şablonları, otomatik gönderi) beta kullanıcı geri bildirimine göre önceliklendirilecek. Vizyon landing'de "Yakında" badge ile bildirildi ama kod eklenmedi.

### Karma yaklaşım — 4 haftalık plan (F.M.Arch.1 sonrası, şirket kurulumu paralel)

- **Hafta 1:** ✅ **TAMAMLANDI**
  - F.M.10a Post Templates MVP ✅ (format-first üretim, 6 format, aspect ratio converter, SignalR)
  - F.7.4 Landing kalan görseller (feature-face-lock, feature-multi-model, og-image) — **kullanıcı yapacak**
- **Hafta 2:** ✅ **TAMAMLANDI**
  - F.9a admin panel genişletme ✅ (Dashboard + paginated user list + user detail + transaction history)
  - F.M.10b Post Templates gelişmiş ✅ (text overlay: server-side ImageSharp v2.1, 5 preset templates statik katalog, Character LoRA + Templates entegrasyonu)
- **Hafta 3 (yarısı tamamlandı):**
  - F.M.UI.2 Studio polish ✅ **TAMAMLANDI** (bakiye ViewComponent tüm sayfalarda + Studio/Image 720px mobile + regenerate frontend re-submit)
  - F.9b admin genişletme — **SIRADAKİ** (2 gün)
- **Hafta 4 (plan korunuyor):**
  - F.M.10c Müzik + Albüm Kapağı — 2-3 gün
  - F.7.5 Legal sayfalar + FAQ + bugfix — 1-2 gün
- **F.7.4** (landing kalan görseller: feature-face-lock, feature-multi-model, og-image) — beta launch öncesi son gün, **kullanıcı hazırlayacak**
- **Buffer:** 3-4 gün şirket kurulum + tampon

### F.M.10b scope (Hafta 2 — Post Templates gelişmiş) ✅ TAMAMLANDI

- **Text overlay:** ✅ server-side ImageSharp v2.1 ile, prompt üretimi SONRASI görsele metin ekleme (`TextOverlayService`, PostProcess akışı).
- **Preset templates:** ✅ 5 preset (statik `PresetTemplateCatalogService`: layout config + text field + prompt suffix + endpoint + aspect ratio).
- **Character LoRA + Templates:** ✅ `StartGenerationRequest.CharacterId` propagate, orchestrator LoRA URL resolve + Ready/ownership validation (controller'da tekrar validation yok).
- **Custom aspect ratio:** preset başına aspect ratio config'inden gelir.

### F.M.10c Music kararları (kilitli)

- **Mode:** Backing Track (15-60sn enstrümantal) + Full Song (30-180sn vokal+lyrics), kullanıcı seçer.
- **Albüm kapağı:** otomatik üretim, prompt'tan yola çıkarak.
- **Kapak formatları:** kullanıcı seçer (1:1, 9:16, 16:9, 4:5).
- **Provider:** Sonilo (ticari lisanslı, Shutterstock kataloğu). MiniMax ve ACE-Step F.M.10c sonrası eklenebilir.

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

## 10. Marka mesajlaşması (Brand Messaging)

Landing hero copy'si kalıcı olarak dokümante edilmiştir (F.7.2). Bu metinler landing'in ana değer önerisidir — tutarlılık için kaynak referans burasıdır.

- **Ana başlık:** "TikTok, Instagram, YouTube için içerik üretimini basitleştir"
- **Alt yazı:** "Karakter tutarlılığı, yüz kopyalama ve 10+ AI model ile sosyal medya görsellerini dakikalar içinde üret. Video ve otomatik gönderi — yakında."
- **Vizyon:** SelfAI sadece AI görsel üretimi değil, sosyal medya için içerik pipeline'ı. Şu an: görsel + karakter + face lock. Yakında: video, TikTok/Reels şablonları, otomatik gönderi zamanlama.
- **Değişim kuralı:** Bu mesajlaşma değişirse (pivot, rebranding) CLAUDE.md güncellenir.

---

## TECH DEBT

**MARS warning in payment flows** — "Savepoints disabled because MARS enabled." Manual rollback handling expected. Consider refactor to disable MARS or restructure DB context if transactional safety becomes a concern. Not critical now due to existing idempotency + DB lock.

**Stripe.net + Iyzipay paralel kullanım** — İki ödeme sağlayıcı paralel aktif. Webhook/callback handling'i her ikisinde de idempotent. Tek bir başarısız ödeme rollback'i için ikisinde de unit test ileride yazılmalı.

**fal.ai cost monitoring** — Admin paneli (F.9) yapılana kadar fal.ai havuz kapasitesi manuel izleniyor. Aşırı satım riski (kullanıcılara dağıtılan toplam credit > fal.ai havuzu) admin'in sorumluluğunda.

---

## BUGFIX HISTORY

**FalAi image response nullable Width/Height (F.M.Arch.1 test sırasında bulundu)**

- fal.ai bazı modeller (nano-banana) image response'ta `width`/`height` null döndürür.
- DTO'da `Width`/`Height` `int?` (nullable) tanımlanır.
- `DynamicImageGenerator`, `FluxLoraGenerator`, `FluxPulidGenerator` `Width`/`Height` için `?? 0` fallback kullanır.
- Downstream (`Generation` entity, DB) 0 kaydeder.

**SixLabors.Fonts SABİT 1.0.0-beta18 — yükseltme YASAK (F.M.10b test sırasında bulundu)**

- `ImageSharp.Drawing` 1.0.0-beta15, `Fonts` **1.0.0-beta18**'e karşı derlendi (nuspec dependency).
- `Fonts` 1.0.x (1.0.1) `IGlyphRenderer.BeginGlyph` imzasını değiştirdi → Drawing beta15'in
  `CachingGlyphRenderer`'ı yeni interface'i implement etmiyor. Sonuç: `DrawText` çağrısında
  **runtime `TypeLoadException: Method 'BeginGlyph' ... does not have an implementation`**.
- Semptom sinsi: `dotnet build` 0 hata verir (public API uyumlu), hata yalnızca text overlay
  render edilirken (F.M.10b preset PostProcess) patlar.
- Çözüm: `.csproj`'da `SixLabors.Fonts` **tam sürüm** `1.0.0-beta18` pinli (`1.0.*` float YASAK).
  Drawing beta15 kullanıldığı sürece Fonts bu sürümde kalır.
- Doğrulama: Inter TTF + ImageSharp 2.1.13 + Fonts beta18 ile Türkçe glifler (ç ğ ı ö ş ü ÇĞİŞÖÜ)
  doğru render ediliyor (izole test + görsel inceleme ile onaylandı).
- Not: Inter-SemiBold.ttf ayrı family "Inter SemiBold" olarak yüklenir; `TextOverlayService`
  lookup'ı "-SemiBold" ekini strip edip "Inter" family'sini bulduğu için SemiBold alanlar
  Regular ağırlıkta render olur (crash değil, kozmetik degradation — Bold ve Regular doğru).