using System.Text.Json.Serialization;

namespace Sat.Api.Entidades.Consultas;

/// <summary>
/// Resumen de movimientos por NIT en una fecha dada: IVA emitido y IVA recibido.
/// </summary>
public class ResumenIVA
{
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("movimientos")]
    public List<MovimientoIVA> Movimientos { get; set; } = new();

    [JsonPropertyName("totales")]
    public TotalesIVA Totales { get; set; } = new();
}

public class MovimientoIVA
{
    [JsonPropertyName("nit")]
    public string Nit { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("facturasEmitidas")]
    public int FacturasEmitidas { get; set; }

    [JsonPropertyName("facturasRecibidas")]
    public int FacturasRecibidas { get; set; }

    /// <summary>IVA de las facturas emitidas (todas, incluye las rechazadas).</summary>
    [JsonPropertyName("ivaEmitido")]
    public decimal IvaEmitido { get; set; }

    /// <summary>IVA de las facturas recibidas (todas, incluye las rechazadas).</summary>
    [JsonPropertyName("ivaRecibido")]
    public decimal IvaRecibido { get; set; }

    /// <summary>IVA de las facturas emitidas autorizadas.</summary>
    [JsonPropertyName("ivaEmitidoAutorizado")]
    public decimal IvaEmitidoAutorizado { get; set; }

    /// <summary>IVA de las facturas recibidas autorizadas.</summary>
    [JsonPropertyName("ivaRecibidoAutorizado")]
    public decimal IvaRecibidoAutorizado { get; set; }
}

public class TotalesIVA
{
    [JsonPropertyName("ivaEmitido")]
    public decimal IvaEmitido { get; set; }

    [JsonPropertyName("ivaRecibido")]
    public decimal IvaRecibido { get; set; }

    [JsonPropertyName("ivaEmitidoAutorizado")]
    public decimal IvaEmitidoAutorizado { get; set; }

    [JsonPropertyName("ivaRecibidoAutorizado")]
    public decimal IvaRecibidoAutorizado { get; set; }
}

/// <summary>
/// Valor autorizado por cada fecha dentro de un rango (con IVA o sin IVA).
/// </summary>
public class ResumenRango
{
    [JsonPropertyName("desde")]
    public string? Desde { get; set; }

    [JsonPropertyName("hasta")]
    public string? Hasta { get; set; }

    /// <summary>"total" = con IVA, "sinIva" = solo el valor.</summary>
    [JsonPropertyName("tipoValores")]
    public string TipoValores { get; set; } = "total";

    [JsonPropertyName("porFecha")]
    public List<ValorPorFecha> PorFecha { get; set; } = new();

    [JsonPropertyName("totalRango")]
    public decimal TotalRango { get; set; }
}

public class ValorPorFecha
{
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("valorAutorizado")]
    public decimal ValorAutorizado { get; set; }

    [JsonPropertyName("facturasAutorizadas")]
    public int FacturasAutorizadas { get; set; }
}

/// <summary>Contribuyente registrado en el RTU (catálogo para consultas).</summary>
public class ContribuyenteConsulta
{
    [JsonPropertyName("nit")]
    public string Nit { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}
