using SelfAI.Models;
using SelfAI.Services.Concretes;
using SelfAI.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IRenderNetAssetService, RenderNetAssetService>();
builder.Services.AddHttpClient<IRenderNetGenerationService, RenderNetGenerationService>();
builder.Services.AddHttpClient<IRenderNetCharacterService, RenderNetCharacterService>();

// RenderNet API ayarlarını yapılandırma(konfigürasyon)
builder.Services.Configure<RenderNetSettings>(builder.Configuration.GetSection("RenderNetApiConnection"));



//Add Seasons
builder.Services.AddSession(options=>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5); // Oturumun 5 dakika sonra zaman aşımına uğramasını sağlar
    options.Cookie.HttpOnly = true; // Çerezlerin JavaScript tarafından erişilmemesini sağlar
    options.Cookie.IsEssential = true; // Oturum çerezinin gerekli olduğunu belirtir
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession(); // Oturum yönetimini etkinleştirir
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

//app.MapDefaultControllerRoute();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=RenderNet}/{action=Index}/{id?}");


app.Run();
