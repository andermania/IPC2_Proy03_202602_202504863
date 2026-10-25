using System.Text.Json;

namespace Sat.Web.Servicios;

/// <summary>
/// Cliente del Servicio 2 (API HTTP). Centraliza el consumo de los endpoints de
/// reportes y consultas que utiliza la aplicación frontend (Programa 1).
/// </summary>
public class ClienteSat
{
    /// <summary>Opciones de deserialización de las respuestas del API.</summary>
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;

    public ClienteSat(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Reporte de facturas y errores por rango de fechas.</summary>
    public Task<ReporteErroresDto> ReporteErroresAsync(string desde, string hasta)
        => ObtenerAsync<ReporteErroresDto>(
            $"/api/reportes/errores?desde={Codificar(desde)}&hasta={Codificar(hasta)}");

    /// <summary>Facturas autorizadas y monto facturado de un contribuyente por fecha.</summary>
    public Task<ReporteContribuyenteDto> ConsultarContribuyenteAsync(string nit, string tipo, string desde, string hasta)
        => ObtenerAsync<ReporteContribuyenteDto>(
            $"/api/reportes/contribuyente?nit={Codificar(nit)}&tipo={Codificar(tipo)}&desde={Codificar(desde)}&hasta={Codificar(hasta)}");

    /// <summary>IVA emitido y recibido por NIT en una fecha.</summary>
    public Task<ResumenIvaDto> ResumenIvaAsync(string fecha)
        => ObtenerAsync<ResumenIvaDto>($"/api/reportes/iva?fecha={Codificar(fecha)}");

    /// <summary>Valor autorizado por fecha dentro de un rango (con IVA o sin IVA).</summary>
    public Task<ResumenRangoDto> ResumenRangoAsync(string desde, string hasta, string tipoValores)
        => ObtenerAsync<ResumenRangoDto>(
            $"/api/reportes/rango?desde={Codificar(desde)}&hasta={Codificar(hasta)}&tipoValores={Codificar(tipoValores)}");

    /// <summary>Catálogo de contribuyentes del RTU (para los listados de NIT).</summary>
    public Task<List<ContribuyenteDto>> CatalogoNitsAsync()
        => ObtenerAsync<List<ContribuyenteDto>>("/api/reportes/nits");

    private async Task<T> ObtenerAsync<T>(string ruta)
    {
        HttpResponseMessage respuesta;

        try
        {
            respuesta = await _http.GetAsync(ruta);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "No fue posible comunicarse con el Servicio 2 (API). Verifique que esté ejecutándose. Detalle: "
                + ex.Message, ex);
        }

        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        if (!respuesta.IsSuccessStatusCode)
        {
            var mensaje = ExtraerError(cuerpo)
                ?? $"El API respondió HTTP {(int)respuesta.StatusCode}.";
            throw new InvalidOperationException(mensaje);
        }

        var datos = JsonSerializer.Deserialize<T>(cuerpo, Opciones);

        return datos is null
            ? throw new InvalidOperationException("El API devolvió una respuesta vacía.")
            : datos;
    }

    private static string? ExtraerError(string cuerpo)
    {
        try
        {
            using var documento = JsonDocument.Parse(cuerpo);
            if (documento.RootElement.ValueKind == JsonValueKind.Object &&
                documento.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString();
            }
        }
        catch (JsonException)
        {
            // La respuesta no es JSON: no se interpreta como mensaje de error.
        }

        return null;
    }

    private static string Codificar(string valor) => Uri.EscapeDataString(valor ?? string.Empty);
}

// ---------------------------------------------------------------------------
// Modelos de las respuestas del API (espejo de la estructura JSON de salida).
// ---------------------------------------------------------------------------

/// <summary>Reporte de facturas recibidas y errores para un rango de fechas.</summary>
public class ReporteErroresDto
{
    public string? Desde { get; set; }
    public string? Hasta { get; set; }
    public int FacturasSinFecha { get; set; }
    public List<ResumenDiarioErroresDto> Fechas { get; set; } = new();
    public TotalesReporteErroresDto Totales { get; set; } = new();
}

public class ResumenDiarioErroresDto
{
    public string Fecha { get; set; } = string.Empty;
    public int CantidadFacturasRecibidas { get; set; }
    public ConteoErroresDto Errores { get; set; } = new();
    public int FacturasSinErrores { get; set; }
    public int EmisoresDistintos { get; set; }
    public int ReceptoresDistintos { get; set; }
}

public class TotalesReporteErroresDto
{
    public int CantidadFacturasRecibidas { get; set; }
    public ConteoErroresDto Errores { get; set; } = new();
    public int FacturasSinErrores { get; set; }
    public int EmisoresDistintos { get; set; }
    public int ReceptoresDistintos { get; set; }
}

public class ConteoErroresDto
{
    public int NitEmisorInvalido { get; set; }
    public int NitReceptorInvalido { get; set; }
    public int NitEmisorInexistente { get; set; }
    public int NitReceptorInexistente { get; set; }
    public int IvaMalCalculado { get; set; }
    public int TotalMalCalculado { get; set; }
    public int ReferenciaDuplicada { get; set; }
    public int ReferenciaInvalida { get; set; }
    public int FechaInvalida { get; set; }
    public int ValorInvalido { get; set; }

    /// <summary>Categorías de error descritas en el enunciado, en su orden.</summary>
    public IEnumerable<(string Etiqueta, int Cantidad)> Categorias()
    {
        yield return ("NIT emisor inválido", NitEmisorInvalido);
        yield return ("NIT receptor inválido", NitReceptorInvalido);
        yield return ("NIT emisor inexistente", NitEmisorInexistente);
        yield return ("NIT receptor inexistente", NitReceptorInexistente);
        yield return ("IVA mal calculado", IvaMalCalculado);
        yield return ("TOTAL mal calculado", TotalMalCalculado);
        yield return ("REFERENCIA duplicada", ReferenciaDuplicada);
    }

    /// <summary>Otras validaciones del servicio (no listadas en el enunciado).</summary>
    public IEnumerable<(string Etiqueta, int Cantidad)> Otros()
    {
        yield return ("REFERENCIA inválida", ReferenciaInvalida);
        yield return ("FECHA inválida", FechaInvalida);
        yield return ("VALOR inválido", ValorInvalido);
    }

    /// <summary>Cantidad total de errores registrados.</summary>
    public int TotalErrores()
        => Categorias().Sum(c => c.Cantidad) + Otros().Sum(c => c.Cantidad);
}

/// <summary>Consulta de un contribuyente: facturas por fecha y total del rango.</summary>
public class ReporteContribuyenteDto
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Desde { get; set; }
    public string? Hasta { get; set; }
    public bool Encontrado { get; set; }
    public List<ResumenContribuyenteFechaDto> PorFecha { get; set; } = new();
    public ResumenContribuyenteFechaDto TotalRango { get; set; } = new();
}

public class ResumenContribuyenteFechaDto
{
    public string Fecha { get; set; } = string.Empty;
    public int FacturasAutorizadas { get; set; }
    public decimal MontoAutorizado { get; set; }
    public int FacturasRecibidas { get; set; }
    public decimal MontoRecibido { get; set; }
}

/// <summary>Resumen de IVA emitido y recibido por NIT en una fecha.</summary>
public class ResumenIvaDto
{
    public string Fecha { get; set; } = string.Empty;
    public List<MovimientoIvaDto> Movimientos { get; set; } = new();
    public TotalesIvaDto Totales { get; set; } = new();
}

public class MovimientoIvaDto
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int FacturasEmitidas { get; set; }
    public int FacturasRecibidas { get; set; }
    public decimal IvaEmitido { get; set; }
    public decimal IvaRecibido { get; set; }
    public decimal IvaEmitidoAutorizado { get; set; }
    public decimal IvaRecibidoAutorizado { get; set; }
}

public class TotalesIvaDto
{
    public decimal IvaEmitido { get; set; }
    public decimal IvaRecibido { get; set; }
    public decimal IvaEmitidoAutorizado { get; set; }
    public decimal IvaRecibidoAutorizado { get; set; }
}

/// <summary>Valor autorizado por fecha dentro de un rango de fechas.</summary>
public class ResumenRangoDto
{
    public string? Desde { get; set; }
    public string? Hasta { get; set; }
    public string TipoValores { get; set; } = "total";
    public List<ValorPorFechaDto> PorFecha { get; set; } = new();
    public decimal TotalRango { get; set; }
}

public class ValorPorFechaDto
{
    public string Fecha { get; set; } = string.Empty;
    public decimal ValorAutorizado { get; set; }
    public int FacturasAutorizadas { get; set; }
}

/// <summary>Contribuyente del RTU (catálogo de NIT para las consultas).</summary>
public class ContribuyenteDto
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
