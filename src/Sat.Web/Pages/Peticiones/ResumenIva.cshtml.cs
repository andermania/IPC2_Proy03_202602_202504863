using Microsoft.AspNetCore.Mvc;
using Sat.Web.Servicios;

namespace Sat.Web.Pages;

/// <summary>
/// Peticiones / Resumen de IVA por fecha y NIT: por cada NIT muestra el IVA
/// emitido y el IVA recibido en la fecha seleccionada.
/// </summary>
public class ResumenIvaModel : PaginaReporte
{
    public ResumenIvaModel(ClienteSat api)
        : base(api)
    {
    }

    [BindProperty(Name = "fecha", SupportsGet = true)]
    public string? Fecha { get; set; }

    /// <summary>Resumen de la fecha consultada (null si aún no se ha consultado).</summary>
    public ResumenIvaDto? Resultado { get; private set; }

    public async Task OnGetAsync()
    {
        Fecha ??= DateTime.Today.ToString("yyyy-MM-dd");

        await CargarCatalogoAsync();

        if (string.IsNullOrWhiteSpace(Fecha))
        {
            return;
        }

        await ConsultarAsync(async () =>
        {
            Resultado = await Api.ResumenIvaAsync(Fecha);
        });
    }
}
