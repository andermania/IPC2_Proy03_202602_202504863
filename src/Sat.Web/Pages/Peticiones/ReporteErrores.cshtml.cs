using Microsoft.AspNetCore.Mvc;
using Sat.Web.Servicios;

namespace Sat.Web.Pages;

/// <summary>
/// Peticiones / Reporte de facturas y errores por rango de fechas: total de facturas
/// recibidas, detalle de errores por categoría, facturas sin errores y cantidad de
/// emisores y receptores distintos, desglosado por cada fecha.
/// </summary>
public class ReporteErroresModel : PaginaReporte
{
    public ReporteErroresModel(ClienteSat api)
        : base(api)
    {
    }

    [BindProperty(Name = "desde", SupportsGet = true)]
    public string? Desde { get; set; }

    [BindProperty(Name = "hasta", SupportsGet = true)]
    public string? Hasta { get; set; }

    /// <summary>Reporte de errores del rango consultado (null si aún no se ha consultado).</summary>
    public ReporteErroresDto? Resultado { get; private set; }

    public async Task OnGetAsync()
    {
        var rango = RangoPorDefecto();
        Desde ??= rango.Desde;
        Hasta ??= rango.Hasta;

        await CargarCatalogoAsync();

        await ConsultarAsync(async () =>
        {
            Resultado = await Api.ReporteErroresAsync(Desde!, Hasta!);
        });
    }
}
