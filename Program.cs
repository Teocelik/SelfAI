using Amazon.Runtime;
using Amazon.S3;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SelfAI.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SelfAI.BackgroundServices;
using SelfAI.Configurations;
using SelfAI.Data;
using SelfAI.Hubs;
using SelfAI.Middlewares;
using SelfAI.Services.Concretes;
using SelfAI.Services.Interfaces;
using SelfAI.Services.Generation.Abstractions;
using SelfAI.Services.Generation.Providers.FalAi;
using SelfAI.Services.Generation.Providers.S3;
using SelfAI.Services.Generation.Pricing;
using SelfAI.Services.Generation.Domain.Image;
using SelfAI.Services.Generation.Domain.Catalog;
using SelfAI.Services.Generation.Domain.CharacterTraining;
using SelfAI.Services.Generation.Orchestrators;


var builder = WebApplication.CreateBuilder(args);

// ═══ Firebase Admin SDK initialization ═══
// Service account JSON yolu User Secrets'ten gelir. Server tarafında ID token doğrulamak için gerekli.
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];
if (string.IsNullOrWhiteSpace(firebaseCredentialsPath))
{
    throw new InvalidOperationException(
        "Firebase:CredentialsPath konfigürasyonu bulunamadı. " +
        "User Secrets, appsettings.json veya Firebase__CredentialsPath env variable'ında " +
        "tanımlanmalı (Firebase Admin SDK service account JSON dosyasının yolu)."
    );
}

if (!File.Exists(firebaseCredentialsPath))
{
    throw new FileNotFoundException(
        $"Firebase credentials dosyası bulunamadı: {firebaseCredentialsPath}. " +
        "Dosyayı proje köküne koyduğunuzdan emin olun."
    );
}

// DefaultInstance null kontrolü: hot reload / test çalıştırmalarında double-init hatasını önler.
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
    });
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// IMemoryCache — F.M.5 model catalog cache (15 dk sliding) için.
builder.Services.AddMemoryCache();

// SignalR ekle
builder.Services.AddSignalR();

// SignalR'ın Clients.User(userId) altyapısını Firebase UID'ye bağla.
// Böylece bir kullanıcının TÜM aktif bağlantılarına (multi-tab) tek seferde yayın yapılabilir.
builder.Services.AddSingleton<IUserIdProvider, FirebaseUserIdProvider>();

// fal.ai provider registrations (F.M.2+ phases)
builder.Services.Configure<FalAiOptions>(
    builder.Configuration.GetSection(FalAiOptions.SectionName));

builder.Services.AddHttpClient<IFalAiClient, FalAiClient>();

// fal.ai storage (asset upload) — F.M.4 minimal. F.M.7'de FalAiAssetStorageProvider wrap eder.
builder.Services.AddHttpClient<IFalAiStorageClient, FalAiStorageClient>();

// ═══ F.M.7 — Asset storage abstraction + persistent Asset servisi ═══
// F.7.1 — Environment-based storage provider seçimi (runtime toggle YOK, startup'ta kararlaştırılır):
//   Development + Staging: fal.ai (24 saat retention, test için yeterli)
//   Production: Cloudflare R2 (sonsuz retention, presigned URL 24 saat)

// Fail-fast: Production ortamında R2 config eksikse startup'ta net hata ver.
// Configure<R2Options> bind'inden ÖNCE çalışır.
if (builder.Environment.IsProduction())
{
    var r2Section = builder.Configuration.GetSection(R2Options.SectionName);
    var r2AccessKey = r2Section["AccessKeyId"];
    var r2SecretKey = r2Section["SecretAccessKey"];
    var r2Endpoint = r2Section["Endpoint"];
    var r2Bucket = r2Section["BucketName"];

    if (string.IsNullOrEmpty(r2AccessKey) ||
        string.IsNullOrEmpty(r2SecretKey) ||
        string.IsNullOrEmpty(r2Endpoint) ||
        string.IsNullOrEmpty(r2Bucket))
    {
        throw new InvalidOperationException(
            "Production ortamında R2 konfigürasyonu eksik. R2:AccessKeyId, " +
            "R2:SecretAccessKey, R2:Endpoint ve R2:BucketName User Secrets " +
            "veya appsettings.Production.json içinde tanımlı olmalı.");
    }
}

builder.Services.Configure<R2Options>(
    builder.Configuration.GetSection(R2Options.SectionName));

// IAmazonS3 singleton (thread-safe, connection reuse). R2 endpoint config'liyse kaydedilir.
// Development'ta genelde kayıt olmaz; S3AssetStorageProvider yalnızca Production'da register
// edildiği için sorun değil.
if (!string.IsNullOrEmpty(builder.Configuration[$"{R2Options.SectionName}:Endpoint"]))
{
    builder.Services.AddSingleton<IAmazonS3>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<R2Options>>().Value;

        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,          // R2 için gerekli — virtual-hosted style desteklenmez.
            AuthenticationRegion = "auto"   // R2 region konsepti yok, "auto" standart.
        };

        var credentials = new BasicAWSCredentials(
            options.AccessKeyId,
            options.SecretAccessKey);

        return new AmazonS3Client(credentials, config);
    });
}

// Provider seçimi environment'a göre (D — Dependency Inversion: consumer'lar IAssetStorageProvider'a bağlı).
if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<IAssetStorageProvider, S3AssetStorageProvider>();
}
else
{
    builder.Services.AddScoped<IAssetStorageProvider, FalAiAssetStorageProvider>();
}

builder.Services.AddScoped<IAssetService, AssetService>();

// ═══ F.M.3 — Image generation katmanı ═══
// Credit pricing (tier markup) — config "CreditPricing" section'ından okunur.
builder.Services.Configure<CreditPricingOptions>(
    builder.Configuration.GetSection(CreditPricingOptions.SectionName));
builder.Services.AddScoped<ICreditPricingService, CreditPricingService>();

// ═══ F.M.5 — Dynamic catalog ═══
// Tier resolution artık DB-driven (ModelCatalogEntry.Tier). Model-spesifik default'lar stateless.
builder.Services.AddScoped<ICatalogTierResolver, CatalogTierResolver>();
builder.Services.AddSingleton<IModelDefaultProvider, ModelDefaultProvider>();

// fal.ai unified model list client (catalog sync için — api.fal.ai/v1/models).
builder.Services.AddHttpClient<IFalAiModelCatalogClient, FalAiModelCatalogClient>();

// Catalog orchestrator (cache + favoriler + sync). IMemoryCache aşağıda kayıtlı.
builder.Services.AddScoped<IModelCatalogOrchestrator, ModelCatalogOrchestrator>();

// Image generator'lar (F.M.5): generic DynamicImageGenerator + karakter özel FluxLoraGenerator.
// Eski FluxSchnell/FluxDev + IImageGenerator interface'i kaldırıldı.
builder.Services.AddScoped<DynamicImageGenerator>();
builder.Services.AddScoped<FluxLoraGenerator>();  // Karakter LoRA inference (orchestrator seçer).
builder.Services.AddScoped<FluxPulidGenerator>();        // F.M.6 — Face Lock (orchestrator seçer).

// Generation orchestrator — kredi düşme + history + SignalR koordinasyonu.
builder.Services.AddScoped<IGenerationOrchestrator, GenerationOrchestrator>();

// ═══ F.M.10a — Post Templates (format-first) ═══
// Statik format kataloğu (singleton — stateless). Ayrı orchestrator YOK: controller
// TemplateStartRequestBuilder ile StartGenerationRequest kurup mevcut
// IGenerationOrchestrator'ı çağırır (kredi/log/SignalR tekil, DRY).
builder.Services.AddSingleton<ITemplateCatalogService, TemplateCatalogService>();

// ═══ F.M.10b — Post Templates gelişmiş (preset + text overlay + karakter LoRA) ═══
// Statik preset kataloğu (singleton — stateless). Text overlay servisi singleton:
// font koleksiyonu startup'ta bir kez yüklenir (thread-safe read-only). IHttpClientFactory
// kaynak görseli indirir (default client — typed client'lar factory'yi zaten kayıt eder).
builder.Services.AddSingleton<IPresetTemplateCatalogService, PresetTemplateCatalogService>();
builder.Services.AddSingleton<ITextOverlayService, TextOverlayService>();
builder.Services.AddHttpClient();

// ═══ F.M.4 — Character LoRA training katmanı ═══
// Domain trainer (fal.ai flux-lora-fast-training) + training orchestrator.
builder.Services.AddScoped<ICharacterTrainer, FluxLoraTrainer>();
builder.Services.AddScoped<ICharacterTrainingOrchestrator, CharacterTrainingOrchestrator>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IPromptService, PromptService>();

// Firebase token doğrulama servisi (state taşımıyor, FirebaseAuth.DefaultInstance zaten singleton)
builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();

// ═══ EF Core — SQL Server (LocalDB) ═══
// Connection string User Secrets'ten gelir (ConnectionStrings:DefaultConnection).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection konfigürasyonu bulunamadı. " +
                "User Secrets, appsettings.json veya ConnectionStrings__DefaultConnection " +
                "env variable'ında tanımlanmalı.")
    )
);

// Firebase login sonrası AppUser oluştur/sync eden servis (DbContext scoped olduğu için scoped).
builder.Services.AddScoped<IUserService, UserService>();

// Kredi (token) cüzdanı işlemleri: atomik düşüm, iade, bakiye sorgulama (DbContext scoped).
builder.Services.AddScoped<ICreditService, CreditService>();

// Generation audit kaydı servisi (DbContext scoped).
builder.Services.AddScoped<IGenerationLogService, GenerationLogService>();

// Paket listeleme servisi — Pricing sayfası aktif paketleri buradan çeker (DbContext scoped).
builder.Services.AddScoped<IPackageService, PackageService>();

// Abonelik yaşam döngüsü: başlatma/aktivasyon/iptal + cüzdan top-up (DbContext scoped).
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Karakter yönetimi business servisi — DB-only (F.6.1 + F.M.4, DbContext scoped).
builder.Services.AddScoped<ICharacterService, CharacterService>();

// Iyzico CheckoutForm ödeme servisi — mevcut IyzicoOptions config'ini kullanır (D.3.2).
builder.Services.AddScoped<IIyzicoService, IyzicoService>();

// ═══ Cookie Authentication ═══
// Firebase ile doğrulanan kullanıcı için server-side oturum çerezi. Endpoint'ler C.2'de bağlanacak.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SelfAI.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // AJAX/API isteklerinde login sayfasına redirect yerine 401/403 döndür.
        // Frontend (apiFetch) 401'i yakalayıp login modal'ını açar.
        options.Events.OnRedirectToLogin = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (IsApiRequest(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// SignalR bağlantı takibi servisi (Singleton). GenerationHub buna bağlı.
// NOT (F.M.8): Artık BackgroundService değil — Affogato queue-polling kaldırıldığı için
// hosted loop'a gerek kalmadı. fal.ai polling'i FalAiClient.SubmitAndWaitAsync içinde.
builder.Services.AddSingleton<GenerationPollingService>();

// F.M.4 — Character LoRA training polling (Singleton + HostedService dual registration:
// orchestrator RegisterTrainingJob'u doğrudan çağırabilsin diye).
builder.Services.AddSingleton<LoraTrainingPollingService>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<LoraTrainingPollingService>());

// Subscription Lifecycle Service (Hosted) — saatte bir: süresi dolan Active'leri Expired yapar,
// stale Pending subscription/payment'ları temizler. Scoped DbContext'i scope factory ile kullanır (D.3.3).
builder.Services.AddHostedService<SubscriptionLifecycleService>();

// Iyzico(�deme y�ntemi) API ayarlar�n� yap�land�rma(konfig�rasyon)
builder.Services.Configure<IyzicoOptions>(builder.Configuration.GetSection(IyzicoOptions.SectionName));

// ═══ F.7.3 — Mini-admin (kredi ekleme + email-whitelist yetkilendirme) ═══
// Admin yetkisi: ASP.NET Core Identity Role YOK (CLAUDE.md #14 — Firebase Auth aktif).
// "Admin" policy, cookie'deki email claim'ini AdminOptions.AllowedEmails'e karşı kontrol eder.
builder.Services.Configure<AdminOptions>(
    builder.Configuration.GetSection(AdminOptions.SectionName));

builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddSingleton<IAuthorizationHandler, AdminAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.Requirements.Add(new AdminRequirement()));
});



//Add Seasons
builder.Services.AddSession(options =>
{
    //options.IdleTimeout = TimeSpan.FromMinutes(35); // Oturumun 35 dakika sonra zaman a��m�na u�ramas�n� sa�lar
    options.Cookie.HttpOnly = true; // �erezlerin JavaScript taraf�ndan eri�ilmemesini sa�lar
    options.Cookie.IsEssential = true; // Oturum �erezinin gerekli oldu�unu belirtir
});

// Loglama
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


var app = builder.Build();

//Global hata yakalama middleware'i
app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();// HTTP gelen iste�i HTTPS'e �evir.
app.UseStaticFiles();// wwwroot klas�r�n� (CSS, JS, Resimler) d��ar�ya a�
app.UseRouting();// Adres y�nlendirme mekanizmas�n� �al��t�r.
app.UseSession(); // Oturum y�netimini etkinle�tirir
app.UseAuthentication();// Kimlik doğrulama (çerezdeki kullanıcıyı çöz). UseAuthorization'dan ÖNCE olmalı.
app.UseAuthorization();// Yetki kontrol� yap (Login olmu� mu?).

// ?? SignalR Hub endpoint'ini map'le
app.MapHub<GenerationHub>("/generationHub");

//app.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=RenderNet}/{action=Index}/{id?}");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}");

//app.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

// AJAX/API isteği mi? (cookie auth redirect davranışını belirlemek için)
// XMLHttpRequest header'ı, JSON Accept header'ı veya korunan API path'leri API isteği sayılır.
static bool IsApiRequest(HttpRequest request)
{
    return request.Headers["X-Requested-With"] == "XMLHttpRequest"
        || request.Headers["Accept"].Any(h => h?.Contains("application/json") == true)
        || request.Path.StartsWithSegments("/Studio")
        || request.Path.StartsWithSegments("/Prompt")
        || request.Path.StartsWithSegments("/Characters");
}
