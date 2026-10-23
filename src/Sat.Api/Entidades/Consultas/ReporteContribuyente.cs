using System.Text.Json.Serialization;

namespace Sat.Api.Entidades.Consultas;

/// <summary>
/// Consulta de un contribuyente: facturas emitidas o recibidas en un rango de fechas,
/// con la cantidad de facturas autorizadas y el monto facturado por cada fecha.
/// </summary>
public class ReporteContribuyente
{
    [JsonPropertyName("nit")]
    public string Nit { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>emitidas | recibidas | ambas</summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("desde")]
    public string? Desde { get; set; }

    [JsonPropertyName("hasta")]
    public string? Hasta { get; set; }

    [JsonPropertyName("encontrado")]
    public bool Encontrado { get; set; }

    [JsonPropertyName("porFecha")]
    public List<ResumenContribuyenteFecha> PorFecha { get; set; } = new();

    [JsonPropertyName("totalRango")]
    public ResumenContribuyenteFecha TotalRango { get; set; } = new();
}

public class ResumenContribuyenteFecha
{
    /// <summary>Fecha dd/MM/yyyy. En el total del rango viene vacío.</summary>
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("facturasAutorizadas")]
    public int FacturasAutorizadas { get; set; }

    /// <summary>Suma del TOTAL de las facturas autorizadas.</summary>
    [JsonPropertyName("montoAutorizado")]
    public decimal MontoAutorizado { get; set; }

    [JsonPropertyName("facturasRecibidas")]
    public int FacturasRecibidas { get; set; }

    /// <summary>Suma del TOTAL de todas las facturas recibidas (incluso con errores).</summary>
    [JsonPropertyName("montoRecibido")]
    public decimal MontoRecibido { get; set; }

    public void Acumular(ResumenContribuyenteFecha otro)
    {
        FacturasAutorizadas += otro.FacturasAutorizadas;
        MontoAutorizado += otro.MontoAutorizado;
        FacturasRecibidas += otro.FacturasRecibidas;
        MontoRecibido += otro.MontoRecibido;
    }
}

/// <summary>Listado detallado de facturas emitidas o recibidas por un NIT.</summary>
public class ListadoFacturas
{
    [JsonPropertyName("nit")]
    public string Nit { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("desde")]
    public string? Desde { get; set; }

    [JsonPropertyName("hasta")]
    public string? Hasta { get; set; }

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("montoTotal")]
    public decimal MontoTotal { get; set; }

    [JsonPropertyName("montoTotalAutorizado")]
    public decimal MontoTotalAutorizado { get; set; }

    [JsonPropertyName("facturas")]
    public List<FacturaListado> Facturas { get; set; } = new();
}

public class FacturaListado
{
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("hora")]
    public string Hora { get; set; } = string.Empty;

    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("nitEmisor")]
    public string NitEmisor { get; set; } = string.Empty;

    [JsonPropertyName("nitReceptor")]
    public string NitReceptor { get; set; } = string.Empty;

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("iva")]
    public decimal Iva { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("aprobada")]
    public bool Aprobada { get; set; }

    [JsonPropertyName("codigoAprobacion")]
    public long? CodigoAprobacion { get; set; }

    [JsonPropertyName("errores")]
    public List<string> Errores { get; set; } = new();
}
