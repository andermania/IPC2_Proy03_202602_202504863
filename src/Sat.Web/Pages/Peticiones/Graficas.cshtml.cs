using Microsoft.AspNetCore.Mvc;
using Sat.Web.Servicios;

namespace Sat.Web.Pages;

/// <summary>
/// Peticiones / Gráficas: gráfica de barras del resumen de IVA por fecha y NIT y
/// gráfica de barras del resumen por rango de fechas. Las gráficas se generan en
/// SVG desde el servidor, por lo que no requieren conexión a Internet.
/// </summary>
public class GraficasModel : PaginaReporte
{
    /// <summary>Cantidad máxima de NIT mostrados en la gráfica de IVA.</summary>
    private const int MaximoNits = 12;

    public GraficasModel(ClienteSat api)
        : base(api)
    {
    }

    [BindProperty(Name = "fecha", SupportsGet = true)]
    public string? Fecha { get; set; }

    [BindProperty(Name = "desde", SupportsGet = true)]
    public string? Desde { get; set; }

    [BindProperty(Name = "hasta", SupportsGet = true)]
    public string? Hasta { get; set; }

    [BindProperty(Name = "tipoValores", SupportsGet = true)]
    public string TipoValores { get; set; } = "total";

    public bool ConIva => TipoValores != "sinIva";

    /// <summary>Gráfica del resumen de IVA (SVG) o null.</summary>
    public string? GraficaIva { get; private set; }

    /// <summary>Gráfica del resumen por rango (SVG) o null.</summary>
    public string? GraficaRango { get; private set; }

    /// <summary>Cantidad de NIT omitidos por exceder el máximo de la gráfica.</summary>
    public int NitsOmitidos { get; private set; }

    public async Task OnGetAsync()
    {
        var rango = RangoPorDefecto();
        Fecha ??= DateTime.Today.ToString("yyyy-MM-dd");
        Desde ??= rango.Desde;
        Hasta ??= rango.Hasta;

        await CargarCatalogoAsync();

        var correcto = await ConsultarAsync(async () =>
        {
            var resumenIva = await Api.ResumenIvaAsync(Fecha);
            var resumenRango = await Api.ResumenRangoAsync(Desde!, Hasta!, ConIva ? "total" : "sinIva");

            GenerarGraficaIva(resumenIva);
            GenerarGraficaRango(resumenRango);
        });

        if (!correcto)
        {
            GraficaIva = null;
            GraficaRango = null;
        }
    }

    private void GenerarGraficaIva(ResumenIvaDto resumen)
    {
        var movimientos = resumen.Movimientos
            .OrderByDescending(m => m.IvaEmitido + m.IvaRecibido)
            .ToList();

        if (movimientos.Count > MaximoNits)
        {
            NitsOmitidos = movimientos.Count - MaximoNits;
            movimientos = movimientos.Take(MaximoNits).ToList();
        }

        // Se ordena alfabéticamente por NIT para una lectura más clara del eje.
        movimientos = movimientos.OrderBy(m => m.Nit).ToList();

        GraficaIva = GraficaSvg.BarrasAgrupadas(
            $"IVA emitido y recibido por NIT - {resumen.Fecha}",
            movimientos.Select(m => m.Nit).ToList(),
            movimientos.Select(m => m.IvaEmitido).ToList(),
            movimientos.Select(m => m.IvaRecibido).ToList());
    }

    private void GenerarGraficaRango(ResumenRangoDto resumen)
    {
        var etiqueta = resumen.TipoValores == "sinIva" ? "valor sin IVA" : "valor total (con IVA)";

        GraficaRango = GraficaSvg.Barras(
            $"Valor autorizado por fecha ({etiqueta}) - {resumen.Desde} a {resumen.Hasta}",
            resumen.PorFecha.Select(f => f.Fecha).ToList(),
            resumen.PorFecha.Select(f => f.ValorAutorizado).ToList(),
            etiqueta == "valor sin IVA" ? "Valor sin IVA" : "Valor total");
    }
}
