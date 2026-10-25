using Microsoft.AspNetCore.Mvc;
using Sat.Web.Servicios;

namespace Sat.Web.Pages;

/// <summary>
/// Peticiones / Reporte en PDF: exporta un documento con los resultados de todas las
/// peticiones (reporte de facturas y errores, consulta de contribuyente, resumen de IVA
/// y resumen por rango de fechas). El PDF se construye en el servidor sin librerías
/// externas, por lo que funciona sin conexión a Internet.
/// </summary>
public class ReportePdfModel : PaginaReporte
{
    public ReportePdfModel(ClienteSat api)
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

    [BindProperty(Name = "tipoValores", SupportsGet = true)]
    public string TipoValores { get; set; } = "total";

    public bool ConIva => TipoValores != "sinIva";

    public async Task OnGetAsync()
    {
        var rango = RangoPorDefecto();
        Desde ??= rango.Desde;
        Hasta ??= rango.Hasta;

        await CargarCatalogoAsync();

        if (string.IsNullOrWhiteSpace(Nit) && Catalogo.Count > 0)
        {
            Nit = Catalogo[0].Nit;
        }
    }

    /// <summary>Genera y descarga el documento PDF con los cuatro reportes.</summary>
    public async Task<IActionResult> OnPostDescargarAsync(
        string? nit,
        string tipo,
        string desde,
        string hasta,
        string tipoValores)
    {
        await CargarCatalogoAsync();

        if (string.IsNullOrWhiteSpace(desde) || string.IsNullOrWhiteSpace(hasta))
        {
            var rango = RangoPorDefecto();
            desde = rango.Desde;
            hasta = rango.Hasta;
        }

        tipo = tipo is "recibidas" or "ambas" ? tipo : "emitidas";
        tipoValores = tipoValores == "sinIva" ? "sinIva" : "total";

        if (string.IsNullOrWhiteSpace(nit) && Catalogo.Count > 0)
        {
            nit = Catalogo[0].Nit;
        }

        var pdf = new GeneradorPdf();

        pdf.Titulo("Superintendencia de Administración Tributaria - SAT");
        pdf.Parrafo(
            "Reporte consolidado de las peticiones del servicio de autorización de " +
            "Documentos Tributarios Electrónicos (DTE).");
        pdf.Par("Generado el", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        pdf.Par("Rango de fechas", $"{desde} a {hasta}");
        pdf.Par("Contribuyente consultado", string.IsNullOrWhiteSpace(nit) ? "(sin NIT)" : nit);
        pdf.Par("Tipo de facturas", tipo);
        pdf.Par("Tipo de valores", tipoValores == "sinIva" ? "sin IVA" : "totales (con IVA)");
        pdf.Espacio(6);

        await SeccionErroresAsync(pdf, desde, hasta);
        await SeccionContribuyenteAsync(pdf, nit, tipo, desde, hasta);
        await SeccionIvaAsync(pdf, desde);
        await SeccionRangoAsync(pdf, desde, hasta, tipoValores);

        pdf.Espacio(10);
        pdf.Linea();
        pdf.Parrafo(
            $"Documento generado por el Programa 1 (Razor Pages) a partir del Servicio 2 (API .NET). " +
            $"Páginas: {pdf.Paginas}.");

        return File(pdf.Construir(), "application/pdf", $"reporte_sat_{desde}_{hasta}.pdf");
    }

    private async Task SeccionErroresAsync(GeneradorPdf pdf, string desde, string hasta)
    {
        pdf.Subtitulo("1. Reporte de facturas y errores");

        ReporteErroresDto? reporte;
        try
        {
            reporte = await Api.ReporteErroresAsync(desde, hasta);
        }
        catch (InvalidOperationException ex)
        {
            pdf.Parrafo("No fue posible obtener el reporte: " + ex.Message);
            return;
        }

        pdf.Par("Facturas recibidas", Formato.Entero(reporte.Totales.CantidadFacturasRecibidas));
        pdf.Par("Facturas sin errores", Formato.Entero(reporte.Totales.FacturasSinErrores));
        pdf.Par("Emisores distintos", Formato.Entero(reporte.Totales.EmisoresDistintos));
        pdf.Par("Receptores distintos", Formato.Entero(reporte.Totales.ReceptoresDistintos));
        pdf.Espacio(4);

        if (reporte.Fechas.Count > 0)
        {
            pdf.Tabla(
                new[] { "FECHA", "Recibidas", "Sin errores", "Emisores", "Receptores" },
                new[] { 0.24, 0.19, 0.19, 0.19, 0.19 },
                reporte.Fechas.Select(d => new[]
                {
                    d.Fecha,
                    Formato.Entero(d.CantidadFacturasRecibidas),
                    Formato.Entero(d.FacturasSinErrores),
                    Formato.Entero(d.EmisoresDistintos),
                    Formato.Entero(d.ReceptoresDistintos),
                }).ToList());
        }

        pdf.Parrafo("Listado de errores del rango completo:");
        pdf.Tabla(
            new[] { "Error", "Cantidad" },
            new[] { 0.7, 0.3 },
            reporte.Totales.Errores.Categorias()
                .Select(c => new[] { c.Etiqueta, Formato.Entero(c.Cantidad) })
                .ToList());

        var otros = reporte.Totales.Errores.Otros().ToList();
        if (otros.Any(o => o.Cantidad > 0))
        {
            pdf.Parrafo(
                "Otros errores registrados: " +
                string.Join(", ", otros.Where(o => o.Cantidad > 0)
                    .Select(o => $"{o.Etiqueta} ({o.Cantidad})")) + ".");
        }
    }

    private async Task SeccionContribuyenteAsync(GeneradorPdf pdf, string? nit, string tipo, string desde, string hasta)
    {
        pdf.Subtitulo("2. Consultar contribuyente");

        if (string.IsNullOrWhiteSpace(nit))
        {
            pdf.Parrafo("No se indicó un NIT para la consulta.");
            return;
        }

        ReporteContribuyenteDto? consulta;
        try
        {
            consulta = await Api.ConsultarContribuyenteAsync(nit, tipo, desde, hasta);
        }
        catch (InvalidOperationException ex)
        {
            pdf.Parrafo("No fue posible consultar el contribuyente: " + ex.Message);
            return;
        }

        pdf.Par("NIT", consulta.Nit);
        pdf.Par("Nombre", string.IsNullOrWhiteSpace(consulta.Nombre) ? "(no registrado en el RTU)" : consulta.Nombre);
        pdf.Par("Tipo", consulta.Tipo);
        pdf.Espacio(4);

        if (consulta.PorFecha.Count == 0)
        {
            pdf.Parrafo("No existen facturas de este contribuyente en el rango seleccionado.");
            return;
        }

        pdf.Tabla(
            new[] { "FECHA", "Facturas autorizadas", "Monto facturado" },
            new[] { 0.3, 0.3, 0.4 },
            consulta.PorFecha.Select(d => new[]
            {
                d.Fecha,
                Formato.Entero(d.FacturasAutorizadas),
                Formato.Moneda(d.MontoAutorizado),
            }).ToList());

        pdf.Par(
            "Total del rango",
            $"{Formato.Entero(consulta.TotalRango.FacturasAutorizadas)} facturas / " +
            $"{Formato.Moneda(consulta.TotalRango.MontoAutorizado)}");
    }

    private async Task SeccionIvaAsync(GeneradorPdf pdf, string fecha)
    {
        pdf.Subtitulo("3. Resumen de IVA por fecha y NIT");

        ResumenIvaDto? resumen;
        try
        {
            resumen = await Api.ResumenIvaAsync(fecha);
        }
        catch (InvalidOperationException ex)
        {
            pdf.Parrafo("No fue posible obtener el resumen de IVA: " + ex.Message);
            return;
        }

        pdf.Par("Fecha", resumen.Fecha);

        if (resumen.Movimientos.Count == 0)
        {
            pdf.Parrafo("No existen movimientos de IVA en la fecha indicada.");
            return;
        }

        pdf.Tabla(
            new[] { "NIT", "Nombre", "IVA emitido", "IVA recibido" },
            new[] { 0.18, 0.38, 0.22, 0.22 },
            resumen.Movimientos.Select(m => new[]
            {
                m.Nit,
                string.IsNullOrWhiteSpace(m.Nombre) ? "-" : m.Nombre,
                Formato.Moneda(m.IvaEmitido),
                Formato.Moneda(m.IvaRecibido),
            }).ToList());

        pdf.Par(
            "Totales",
            $"IVA emitido {Formato.Moneda(resumen.Totales.IvaEmitido)} / " +
            $"IVA recibido {Formato.Moneda(resumen.Totales.IvaRecibido)}");
    }

    private async Task SeccionRangoAsync(GeneradorPdf pdf, string desde, string hasta, string tipoValores)
    {
        pdf.Subtitulo("4. Resumen por rango de fechas");

        ResumenRangoDto? resumen;
        try
        {
            resumen = await Api.ResumenRangoAsync(desde, hasta, tipoValores);
        }
        catch (InvalidOperationException ex)
        {
            pdf.Parrafo("No fue posible obtener el resumen por rango: " + ex.Message);
            return;
        }

        pdf.Par("Tipo de valores", tipoValores == "sinIva" ? "sin IVA" : "totales (con IVA)");

        if (resumen.PorFecha.Count == 0)
        {
            pdf.Parrafo("No existen facturas autorizadas en el rango seleccionado.");
            return;
        }

        var encabezadoValor = tipoValores == "sinIva" ? "Valor sin IVA" : "Valor total";

        pdf.Tabla(
            new[] { "FECHA", "Facturas autorizadas", encabezadoValor },
            new[] { 0.3, 0.3, 0.4 },
            resumen.PorFecha.Select(f => new[]
            {
                f.Fecha,
                Formato.Entero(f.FacturasAutorizadas),
                Formato.Moneda(f.ValorAutorizado),
            }).ToList());

        pdf.Par("Total del rango", Formato.Moneda(resumen.TotalRango));
    }
}
