using SelfAI.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Add Seasons
builder.Services.AddSession(options=>
{
    options.IdleTimeout = TimeSpan.FromMinutes(3); // Oturumun 30 dakika sonra zaman a��m�na u�ramas�n� sa�lar
    options.Cookie.HttpOnly = true; // �erezlerin JavaScript taraf�ndan eri�ilmemesini sa�lar
    options.Cookie.IsEssential = true; // Oturum �erezinin gerekli oldu�unu belirtir
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession(); // Oturum y�netimini etkinle�tirir
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapDefaultControllerRoute();


app.Run();
