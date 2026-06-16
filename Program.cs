using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SelfAI.BackgroundServices;
using SelfAI.Configurations;
using SelfAI.Data;
using SelfAI.Hubs;
using SelfAI.Middlewares;
using SelfAI.Services.Concretes;
using SelfAI.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// ═══ Firebase Admin SDK initialization ═══
// Service account JSON yolu User Secrets'ten gelir. Server tarafında ID token doğrulamak için gerekli.
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"];
if (string.IsNullOrWhiteSpace(firebaseCredentialsPath))
{
    throw new InvalidOperationException(
        "Firebase:CredentialsPath User Secrets'te tanımlı değil. " +
        "Lütfen Firebase Admin SDK service account JSON dosyasının yolunu ekleyin."
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

// SignalR ekle
builder.Services.AddSignalR();

// SignalR'ın Clients.User(userId) altyapısını Firebase UID'ye bağla.
// Böylece bir kullanıcının TÜM aktif bağlantılarına (multi-tab) tek seferde yayın yapılabilir.
builder.Services.AddSingleton<IUserIdProvider, FirebaseUserIdProvider>();

builder.Services.AddHttpClient<IRenderNetAssetService, RenderNetAssetService>();
builder.Services.AddHttpClient<IRenderNetGenerationService, RenderNetGenerationService>();
builder.Services.AddHttpClient<IRenderNetCharacterService, RenderNetCharacterService>();
builder.Services.AddHttpClient<IRenderNetResourcesService, RenderNetResourcesService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IPromptService, PromptService>();

// Firebase token doğrulama servisi (state taşımıyor, FirebaseAuth.DefaultInstance zaten singleton)
builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();

// ═══ EF Core — SQL Server (LocalDB) ═══
// Connection string User Secrets'ten gelir (ConnectionStrings:DefaultConnection).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection User Secrets'te tanımlı değil.")
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

// Karakter yönetimi business servisi — DB + Affogato API orkestrasyonu (F.6.1, DbContext scoped).
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

// Background Polling Service (Singleton olarak �al���r)
builder.Services.AddSingleton<GenerationPollingService>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<GenerationPollingService>());

// Subscription Lifecycle Service (Hosted) — saatte bir: süresi dolan Active'leri Expired yapar,
// stale Pending subscription/payment'ları temizler. Scoped DbContext'i scope factory ile kullanır (D.3.3).
builder.Services.AddHostedService<SubscriptionLifecycleService>();

// RenderNet API ayarlar�n� yap�land�rma(konfig�rasyon)
builder.Services.Configure<RenderNetOptions>(builder.Configuration.GetSection("RenderNetOptions"));
// Iyzico(�deme y�ntemi) API ayarlar�n� yap�land�rma(konfig�rasyon)
builder.Services.Configure<IyzicoOptions>(builder.Configuration.GetSection("IyzicoOptions"));



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

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=RenderNet}/{action=Index}/{id?}");

//app.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Home}/{action=Index}");

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
        || request.Path.StartsWithSegments("/RenderNet")
        || request.Path.StartsWithSegments("/Prompt")
        || request.Path.StartsWithSegments("/Characters");
}
