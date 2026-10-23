var builder = WebApplication.CreateBuilder(args);

// Programa 1 - Aplicación web (Razor Pages) que consume el Servicio 2 (API).
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
