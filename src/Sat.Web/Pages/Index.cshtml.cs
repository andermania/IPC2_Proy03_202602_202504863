using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sat.Web.Pages;

/// <summary>
/// Programa 1 - Pantalla de envío de mensajes al Servicio 2 (API).
/// </summary>
public class IndexModel : PageModel
{
    private readonly IConfiguration _configuracion;

    public IndexModel(IConfiguration configuracion)
    {
        _configuracion = configuracion;
    }

    /// <summary>URL base del Servicio 2 (API).</summary>
    public string ApiBaseUrl { get; private set; } = "http://localhost:5200";

    public void OnGet()
    {
        ApiBaseUrl = _configuracion["ApiBaseUrl"] ?? ApiBaseUrl;
    }
}
