using SelfAI.Configurations;
using SelfAI.Services.Concretes;
using SelfAI.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IRenderNetAssetService, RenderNetAssetService>();
builder.Services.AddHttpClient<IRenderNetGenerationService, RenderNetGenerationService>();
builder.Services.AddHttpClient<IRenderNetCharacterService, RenderNetCharacterService>();
builder.Services.AddHttpClient<IRenderNetResourcesService, RenderNetResourcesService>();
//builder.Services.AddHttpClient<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// RenderNet API ayarlarýný yapýlandýrma(konfigürasyon)
builder.Services.Configure<RenderNetOptions>(builder.Configuration.GetSection("RenderNetOptions"));
builder.Services.Configure<IyzicoOptions>(builder.Configuration.GetSection("IyzicoOptions"));



//Add Seasons
builder.Services.AddSession(options=>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5); // Oturumun 5 dakika sonra zaman aþýmýna uðramasýný saðlar
    options.Cookie.HttpOnly = true; // Çerezlerin JavaScript tarafýndan eriþilmemesini saðlar
    options.Cookie.IsEssential = true; // Oturum çerezinin gerekli olduðunu belirtir
});

var app = builder.Build();

//Global hata yakalama middleware'i
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession(); // Oturum yönetimini etkinleþtirir
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

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
