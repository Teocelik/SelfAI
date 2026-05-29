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
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IPromptService, PromptService>();

// Background Polling Service (Singleton olarak �al���r)
builder.Services.AddSingleton<GenerationPollingService>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<GenerationPollingService>());

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
app.UseAuthorization();// Yetki kontrol� yap (Login olmu� mu?).

// ?? SignalR Hub endpoint'ini map'le
app.MapHub<GenerationHub>("/generationHub");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=RenderNet}/{action=Index}/{id?}");

app.Run();
