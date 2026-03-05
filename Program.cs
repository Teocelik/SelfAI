using SelfAI.BackgroundServices;  
using SelfAI.Configurations;
using SelfAI.Hubs;
using SelfAI.Middlewares;
using SelfAI.Services.Concretes;
using SelfAI.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// SignalR ekle
builder.Services.AddSignalR();

builder.Services.AddHttpClient<IRenderNetAssetService, RenderNetAssetService>();
builder.Services.AddHttpClient<IRenderNetGenerationService, RenderNetGenerationService>();
builder.Services.AddHttpClient<IRenderNetCharacterService, RenderNetCharacterService>();
builder.Services.AddHttpClient<IRenderNetResourcesService, RenderNetResourcesService>();
//builder.Services.AddHttpClient<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Background Polling Service (Singleton olarak çalýþýr)
builder.Services.AddSingleton<GenerationPollingService>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<GenerationPollingService>());

// RenderNet API ayarlarýný yapýlandýrma(konfigürasyon)
builder.Services.Configure<RenderNetOptions>(builder.Configuration.GetSection("RenderNetOptions"));
// Iyzico(Ödeme yöntemi) API ayarlarýný yapýlandýrma(konfigürasyon)
builder.Services.Configure<IyzicoOptions>(builder.Configuration.GetSection("IyzicoOptions"));



//Add Seasons
builder.Services.AddSession(options =>
{
    //options.IdleTimeout = TimeSpan.FromMinutes(35); // Oturumun 35 dakika sonra zaman aþýmýna uðramasýný saðlar
    options.Cookie.HttpOnly = true; // Çerezlerin JavaScript tarafýndan eriþilmemesini saðlar
    options.Cookie.IsEssential = true; // Oturum çerezinin gerekli olduðunu belirtir
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

app.UseHttpsRedirection();// HTTP gelen isteði HTTPS'e çevir.
app.UseStaticFiles();// wwwroot klasörünü (CSS, JS, Resimler) dýþarýya aç
app.UseRouting();// Adres yönlendirme mekanizmasýný çalýþtýr.
app.UseSession(); // Oturum yönetimini etkinleþtirir
app.UseAuthorization();// Yetki kontrolü yap (Login olmuþ mu?).

// ?? SignalR Hub endpoint'ini map'le
app.MapHub<GenerationHub>("/generationHub");

//app.MapStaticAssets();

//app.MapDefaultControllerRoute();
//app.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Payment}/{action=InitializeIyzicoCheckOutForm}/{id?}");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=RenderNet}/{action=Index}/{id?}");

//app.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
