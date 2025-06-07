using SelfAI.Models;
using SelfAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//API i�in gerekli config�rasyonlar...
builder.Services.Configure<FireBaseAuth>(builder.Configuration.GetSection("FireBaseAuth"));

//FireBase Auth servisi i�in http hizmeti(istek g�ndermek i�in)
builder.Services.AddHttpClient<FirebaseAuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapDefaultControllerRoute();


app.Run();
