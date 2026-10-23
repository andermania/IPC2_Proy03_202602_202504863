using System.Text.Json.Serialization;
using Sat.Api.TDA;

namespace Sat.Api.Entidades;

/// <summary>
/// Registro persistido de cada factura recibida por el servicio, con el resultado de su
/// procesamiento (aprobada o rechazada), el código de aprobación cuando corresponde y la
/// lista de errores detectados. Es la fuente de datos de los reportes y consultas.
/// </summary>
public class RegistroFactura
{
    /// <summary>Texto original del campo TIEMPO tal como fue recibido.</summary>
    [JsonPropertyName("tiempo")]
    public string Tiempo { get; set; } = string.Empty;

    /// <summary>Fecha de emisión extraída del campo TIEMPO, formato dd/MM/yyyy. Vacío si no pudo extraerse.</summary>
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    /// <summary>Hora de emisión extraída del campo TIEMPO, formato HH:mm. Vacío si no pudo extraerse.</summary>
    [JsonPropertyName("hora")]
    public string Hora { get; set; } = string.Empty;

    /// <summary>Fecha y hora en que el servicio procesó la solicitud (ISO 8601).</summary>
    [JsonPropertyName("fechaProceso")]
    public string FechaProceso { get; set; } = string.Empty;

    /// <summary>Referencia interna enviada por el emisor.</summary>
    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    /// <summary>NIT del emisor.</summary>
    [JsonPropertyName("nitEmisor")]
    public string NitEmisor { get; set; } = string.Empty;

    /// <summary>NIT del receptor.</summary>
    [JsonPropertyName("nitReceptor")]
    public string NitReceptor { get; set; } = string.Empty;

    /// <summary>VALOR informado en el mensaje.</summary>
    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    /// <summary>IVA informado en el mensaje.</summary>
    [JsonPropertyName("iva")]
    public decimal Iva { get; set; }

    /// <summary>TOTAL informado en el mensaje.</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>Indica si la factura fue autorizada (sin errores).</summary>
    [JsonPropertyName("aprobada")]
    public bool Aprobada { get; set; }

    /// <summary>Código único de autorización yyyymmdd########. Nulo si fue rechazada.</summary>
    [JsonPropertyName("codigoAprobacion")]
    public long? CodigoAprobacion { get; set; }

    /// <summary>Errores detectados en la factura (vacío cuando fue autorizada).</summary>
    [JsonPropertyName("errores")]
    public ListaEnlazada<string> Errores { get; set; } = new();
}
