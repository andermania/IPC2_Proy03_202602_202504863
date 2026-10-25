using Microsoft.AspNetCore.Mvc.RazorPages;
using Sat.Web.Servicios;

namespace Sat.Web.Pages;

/// <summary>
/// Base de las páginas de "Peticiones". Centraliza el acceso al Servicio 2 (API),
/// el catálogo de contribuyentes del RTU y el manejo de errores de comunicación.
/// </summary>
public abstract class PaginaReporte : PageModel
{
    protected PaginaReporte(ClienteSat api)
    {
        Api = api;
    }

    /// <summary>Cliente del Servicio 2 (API).</summary>
    protected ClienteSat Api { get; }

    /// <summary>Contribuyentes del RTU, utilizados para autocompletar los NIT.</summary>
    public List<ContribuyenteDto> Catalogo { get; private set; } = new();

    /// <summary>Mensaje de error mostrado al usuario (API no disponible o consulta inválida).</summary>
    public string? Error { get; protected set; }

    /// <summary>Indica si la consulta ya fue realizada (para no mostrar tablas vacías al ingresar).</summary>
    public bool Consultado { get; protected set; }

    /// <summary>Carga el catálogo de NIT. Un fallo aquí no impide usar la página.</summary>
    protected async Task CargarCatalogoAsync()
    {
        try
        {
            Catalogo = await Api.CatalogoNitsAsync();
        }
        catch (InvalidOperationException)
        {
            Catalogo = new List<ContribuyenteDto>();
        }
    }

    /// <summary>Ejecuta una consulta al API capturando sus mensajes de error.</summary>
    protected async Task<bool> ConsultarAsync(Func<Task> consulta)
    {
        try
        {
            await consulta();
            Consultado = true;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Error = ex.Message;
            Consultado = true;
            return false;
        }
    }

    /// <summary>Rango de fechas por defecto: últimos 30 días.</summary>
    protected static (string Desde, string Hasta) RangoPorDefecto()
    {
        var hoy = DateTime.Today;
        return (
            hoy.AddDays(-30).ToString("yyyy-MM-dd"),
            hoy.ToString("yyyy-MM-dd"));
    }
}
