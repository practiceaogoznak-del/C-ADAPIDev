using FrontEnd.Data;
using FrontEnd.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(4000);
});builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<TaskService>();

builder.Services.AddHttpClient<WeatherForecastClient>(c =>
{
    c.BaseAddress = new Uri("http://localhost:4001");
});

builder.Services.AddHttpClient<AccessRequestClient>(c =>
{
    c.BaseAddress = new Uri("http://localhost:4001");
});

builder.Services.AddHttpClient<AuthenticationService>(c =>
{
    c.BaseAddress = new Uri("http://localhost:4001");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Отключаем HTTPS для разработки
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
