using Microsoft.AspNetCore.Mvc;
using Sat.Web.Servicios;

namespace Sat.Web.Pages;

/// <summary>
/// Peticiones / Resumen por rango de fechas: valor autorizado por cada fecha,
/// con IVA (total) o sin IVA, según lo que el usuario elija.
/// </summary>
public class ResumenRangoModel : PaginaReporte
{
    public ResumenRangoModel(ClienteSat api)
        : base(api)
    {
    }

    [BindProperty(Name = "desde", SupportsGet = true)]
    public string? Desde { get; set; }

    [BindProperty(Name = "hasta", SupportsGet = true)]
    public string? Hasta { get; set; }

    /// <summary>"total" = con IVA, "sinIva" = solo el valor de las facturas.</summary>
    [BindProperty(Name = "tipoValores", SupportsGet = true)]
    public string TipoValores { get; set; } = "total";

    public bool ConIva => TipoValores != "sinIva";

    /// <summary>Resumen del rango consultado (null si aún no se ha consultado).</summary>
    public ResumenRangoDto? Resultado { get; private set; }

    public async Task OnGetAsync()
    {
        var rango = RangoPorDefecto();
        Desde ??= rango.Desde;
        Hasta ??= rango.Hasta;

        await CargarCatalogoAsync();

        await ConsultarAsync(async () =>
        {
            Resultado = await Api.ResumenRangoAsync(Desde!, Hasta!, ConIva ? "total" : "sinIva");
        });
    }
}
