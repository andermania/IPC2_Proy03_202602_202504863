var builder = WebApplication.CreateBuilder(args);

// Programa 1 - Aplicación web (Razor Pages) que consume el Servicio 2 (API).
builder.Services.AddRazorPages();

// Cliente HTTP del Servicio 2 (API de autorización de DTE).
builder.Services.AddHttpClient<Sat.Web.Servicios.ClienteSat>(cliente =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5200";
    cliente.BaseAddress = new Uri(baseUrl);
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
