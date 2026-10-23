using System.Text.Json.Serialization;

namespace Sat.Api.Entidades;

/// <summary>Respuesta a un mensaje de solicitudes de autorización de facturas.</summary>
public class ResultadoAutorizaciones
{
    [JsonPropertyName("resultadoAutorizaciones")]
    public ResultadoAutorizacionesDetalle Detalle { get; set; } = new();
}

/// <summary>Detalle del resultado de las autorizaciones.</summary>
public class ResultadoAutorizacionesDetalle
{
    [JsonPropertyName("cantidadAutorizacionesAprobadas")]
    public int CantidadAutorizacionesAprobadas { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitEmisorInexistente")]
    public int CantidadRechazadasPorNitEmisorInexistente { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitReceptorInexistente")]
    public int CantidadRechazadasPorNitReceptorInexistente { get; set; }

    // Campos adicionales de rechazo (manteniendo la estructura base del enunciado).

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitEmisorInvalido")]
    public int CantidadRechazadasPorNitEmisorInvalido { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitReceptorInvalido")]
    public int CantidadRechazadasPorNitReceptorInvalido { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorIvaIncorrecto")]
    public int CantidadRechazadasPorIvaIncorrecto { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorTotalIncorrecto")]
    public int CantidadRechazadasPorTotalIncorrecto { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorReferenciaDuplicada")]
    public int CantidadRechazadasPorReferenciaDuplicada { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorReferenciaInvalida")]
    public int CantidadRechazadasPorReferenciaInvalida { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorFechaInvalida")]
    public int CantidadRechazadasPorFechaInvalida { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorValorInvalido")]
    public int CantidadRechazadasPorValorInvalido { get; set; }

    [JsonPropertyName("detalleAprobaciones")]
    public List<DetalleAprobacion> DetalleAprobaciones { get; set; } = new();
}

/// <summary>Código de aprobación emitido para cada factura autorizada.</summary>
public class DetalleAprobacion
{
    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("codigoAprobacion")]
    public long CodigoAprobacion { get; set; }
}
