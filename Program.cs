using SelfAI.Models;
using SelfAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//API için gerekli configürasyonlar...
builder.Services.Configure<FireBaseAuth>(builder.Configuration.GetSection("FireBaseAuth"));

//FireBase Auth servisi için http hizmeti(istek göndermek için)
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
