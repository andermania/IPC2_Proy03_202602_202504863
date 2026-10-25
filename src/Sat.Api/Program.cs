using Sat.Api.Datos;
using Sat.Api.Dominio;
using Sat.Api.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Serialización de los mensajes de respuesta.
builder.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    opciones.JsonSerializerOptions.NumberHandling =
        System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
});

// Permite que la aplicación frontend (y cualquier otro cliente, p. ej. Postman) consuma el API.
builder.Services.AddCors(opciones => opciones.AddPolicy("accesoTotal", politica => politica
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Capa de datos: archivos JSON persistidos en la carpeta "App_Data" del repositorio.
// La raíz se busca a partir de la solución para que el servicio guarde exactamente los
// mismos archivos sin importar cómo se ejecute (dotnet run o el ejecutable compilado),
// de modo que la información persistente nunca parezca "perdida".
var rutaDatos = ResolverDirectorioDatos(
    builder.Environment.ContentRootPath,
    builder.Configuration["Datos:Ruta"] ?? "App_Data");
Directory.CreateDirectory(rutaDatos);

builder.Services.AddSingleton(new RepositorioContribuyentes(Path.Combine(rutaDatos, "rtu.json")));
builder.Services.AddSingleton(new RepositorioFacturas(Path.Combine(rutaDatos, "facturas.json")));
builder.Services.AddSingleton(new GeneradorCorrelativo(Path.Combine(rutaDatos, "correlativo.json")));

static string ResolverDirectorioDatos(string contenido, string relativo)
{
    if (Path.IsPathRooted(relativo))
    {
        return relativo;
    }

    var directorio = new DirectoryInfo(contenido);
    while (directorio is not null)
    {
        if (File.Exists(Path.Combine(directorio.FullName, "Proy03.sln")))
        {
            return Path.Combine(directorio.FullName, relativo);
        }

        directorio = directorio.Parent;
    }

    return Path.Combine(contenido, relativo);
}

// Modo de validación del dígito verificador del NIT (por defecto: algoritmo del enunciado).
var modoNIT = builder.Configuration["ValidacionNIT:Modo"] ?? nameof(ValidadorNIT.ModoValidacion.Enunciado);
if (Enum.TryParse<ValidadorNIT.ModoValidacion>(modoNIT, ignoreCase: true, out var modo))
{
    ValidadorNIT.Modo = modo;
}

// Servicios de negocio.
builder.Services.AddSingleton<ServicioRTU>();
builder.Services.AddSingleton<ProcesadorAutorizaciones>();
builder.Services.AddSingleton<ServicioReportes>();

var app = builder.Build();

app.UseCors("accesoTotal");
app.MapControllers();

app.Run();
