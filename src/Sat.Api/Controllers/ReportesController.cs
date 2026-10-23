using Microsoft.AspNetCore.Mvc;
using Sat.Api.Dominio;
using Sat.Api.Servicios;

namespace Sat.Api.Controllers;

/// <summary>
/// Reportes y consultas sobre la información almacenada: reporte de errores por rango de
/// fechas, facturas por contribuyente, resumen de IVA y resumen de valores por rango.
/// </summary>
[ApiController]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly ServicioReportes _reportes;

    public ReportesController(ServicioReportes reportes)
    {
        _reportes = reportes;
    }

    /// <summary>
    /// Reporte de facturas recibidas para un rango de fechas: cantidad total de facturas,
    /// listado de errores por categoría, facturas sin errores y emisores/receptores distintos.
    /// </summary>
    [HttpGet("errores")]
    public ActionResult<Entidades.Consultas.ReporteErrores> Errores(
        [FromQuery] string? desde,
        [FromQuery] string? hasta)
    {
        if (!Fechas.TryParseRango(desde, hasta, out var fechaDesde, out var fechaHasta, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok(_reportes.ReporteErrores(fechaDesde, fechaHasta));
    }

    /// <summary>
    /// Consulta de un contribuyente: por cada fecha, cantidad de facturas autorizadas y
    /// monto facturado, más el total de todo el rango.
    /// </summary>
    [HttpGet("contribuyente")]
    public ActionResult<Entidades.Consultas.ReporteContribuyente> Contribuyente(
        [FromQuery] string nit,
        [FromQuery] string tipo,
        [FromQuery] string? desde,
        [FromQuery] string? hasta)
    {
        if (string.IsNullOrWhiteSpace(nit))
        {
            return BadRequest(new { error = "El parámetro 'nit' es obligatorio." });
        }

        if (!IntentarInterpretarTipo(tipo, out var tipoNormalizado))
        {
            return BadRequest(new { error = "El parámetro 'tipo' debe ser: emitidas, recibidas o ambas." });
        }

        if (!Fechas.TryParseRango(desde, hasta, out var fechaDesde, out var fechaHasta, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok(_reportes.ConsultaContribuyente(nit, tipoNormalizado, fechaDesde, fechaHasta));
    }

    /// <summary>Listado de facturas emitidas o recibidas por un NIT en un rango de fechas.</summary>
    [HttpGet("facturas")]
    public ActionResult<Entidades.Consultas.ListadoFacturas> Facturas(
        [FromQuery] string nit,
        [FromQuery] string tipo,
        [FromQuery] string? desde,
        [FromQuery] string? hasta)
    {
        if (string.IsNullOrWhiteSpace(nit))
        {
            return BadRequest(new { error = "El parámetro 'nit' es obligatorio." });
        }

        if (!IntentarInterpretarTipo(tipo, out var tipoNormalizado))
        {
            return BadRequest(new { error = "El parámetro 'tipo' debe ser: emitidas, recibidas o ambas." });
        }

        if (!Fechas.TryParseRango(desde, hasta, out var fechaDesde, out var fechaHasta, out var error))
        {
            return BadRequest(new { error });
        }

        return Ok(_reportes.ListadoFacturas(nit, tipoNormalizado, fechaDesde, fechaHasta));
    }

    /// <summary>Resumen de movimientos por NIT en una fecha: IVA emitido y recibido.</summary>
    [HttpGet("iva")]
    public ActionResult<Entidades.Consultas.ResumenIVA> Iva([FromQuery] string fecha)
    {
        if (!Fechas.TryParse(fecha, out var fechaConsulta))
        {
            return BadRequest(new { error = "El parámetro 'fecha' es obligatorio y debe tener formato dd/MM/yyyy." });
        }

        return Ok(_reportes.ResumenIVA(fechaConsulta));
    }

    /// <summary>
    /// Valor autorizado por cada fecha en un rango, con IVA (tipoValores=total) o
    /// sin IVA (tipoValores=sinIva).
    /// </summary>
    [HttpGet("rango")]
    public ActionResult<Entidades.Consultas.ResumenRango> Rango(
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        [FromQuery] string? tipoValores)
    {
        if (!Fechas.TryParseRango(desde, hasta, out var fechaDesde, out var fechaHasta, out var error))
        {
            return BadRequest(new { error });
        }

        var conIVA = !string.Equals(tipoValores, "sinIva", StringComparison.OrdinalIgnoreCase);
        return Ok(_reportes.ResumenRango(fechaDesde, fechaHasta, conIVA));
    }

    /// <summary>Catálogo de NITs del RTU (contribuyentes) para las consultas.</summary>
    [HttpGet("nits")]
    public ActionResult<IEnumerable<Entidades.Consultas.ContribuyenteConsulta>> Nits()
    {
        return Ok(_reportes.CatalogoNITs());
    }

    private static bool IntentarInterpretarTipo(string? tipo, out string tipoNormalizado)
    {
        var valor = (tipo ?? string.Empty).Trim().ToLowerInvariant();

        tipoNormalizado = valor switch
        {
            "emitida" or "emitidas" or "emisor" => "emitidas",
            "recibida" or "recibidas" or "receptor" => "recibidas",
            "ambas" or "todas" => "ambas",
            _ => string.Empty,
        };

        return tipoNormalizado.Length > 0;
    }
}
