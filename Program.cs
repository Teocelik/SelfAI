using SelfAI.Models;
using SelfAI.Services.Concretes;
using SelfAI.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IRenderNetService, RenderNetService>();
// RenderNet API ayarlarýný yapýlandýrma(konfigürasyon)
builder.Services.Configure<RenderNetSettings>(builder.Configuration.GetSection("RenderNetApiConnection"));



//Add Seasons
builder.Services.AddSession(options=>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5); // Oturumun 5 dakika sonra zaman aþýmýna uðramasýný saðlar
    options.Cookie.HttpOnly = true; // Çerezlerin JavaScript tarafýndan eriþilmemesini saðlar
    options.Cookie.IsEssential = true; // Oturum çerezinin gerekli olduðunu belirtir
});

var app = builder.Build();

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

app.MapDefaultControllerRoute();


app.Run();
