using System.Text.Json.Serialization;

namespace Sat.Api.Entidades;

/// <summary>Respuesta a un mensaje de solicitudes de autorización de facturas.</summary>
public class ResultadoAutorizaciones
{
    [JsonPropertyName("resultadoAutorizaciones")]
    public ResultadoAutorizacionesDetalle Detalle { get; set; } = new();
}

/// <summary>Detalle del resultado de las autorizaciones.</summary>
/// <remarks>
/// La estructura de este objeto es obligatoria y no puede modificarse: contiene
/// exactamente las llaves descritas en el enunciado del proyecto. El detalle de los
/// errores de cada factura se expone en el reporte de errores (GET /api/reportes/errores).
/// </remarks>
public class ResultadoAutorizacionesDetalle
{
    [JsonPropertyName("cantidadAutorizacionesAprobadas")]
    public int CantidadAutorizacionesAprobadas { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitEmisorInexistente")]
    public int CantidadRechazadasPorNitEmisorInexistente { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitReceptorInexistente")]
    public int CantidadRechazadasPorNitReceptorInexistente { get; set; }

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
