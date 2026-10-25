using Microsoft.AspNetCore.Mvc;
using Sat.Web.Servicios;

namespace Sat.Web.Pages;

/// <summary>
/// Peticiones / Consultar contribuyente: por cada fecha muestra la cantidad de
/// facturas autorizadas (emitidas o recibidas) y el monto facturado, más el
/// total de todo el rango consultado.
/// </summary>
public class ConsultarModel : PaginaReporte
{
    public ConsultarModel(ClienteSat api)
        : base(api)
    {
    }

    [BindProperty(Name = "nit", SupportsGet = true)]
    public string? Nit { get; set; }

    [BindProperty(Name = "tipo", SupportsGet = true)]
    public string Tipo { get; set; } = "emitidas";

    [BindProperty(Name = "desde", SupportsGet = true)]
    public string? Desde { get; set; }

    [BindProperty(Name = "hasta", SupportsGet = true)]
    public string? Hasta { get; set; }

    /// <summary>Resultado de la consulta (null si aún no se ha consultado).</summary>
    public ReporteContribuyenteDto? Resultado { get; private set; }

    public async Task OnGetAsync()
    {
        var rango = RangoPorDefecto();
        Desde ??= rango.Desde;
        Hasta ??= rango.Hasta;

        await CargarCatalogoAsync();

        if (string.IsNullOrWhiteSpace(Nit))
        {
            return;
        }

        await ConsultarAsync(async () =>
        {
            Resultado = await Api.ConsultarContribuyenteAsync(Nit, Tipo, Desde, Hasta);
        });
    }
}
