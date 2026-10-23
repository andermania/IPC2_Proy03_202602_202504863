using System.Text.Json.Serialization;

namespace Sat.Api.Entidades;

/// <summary>
/// Solicitud de autorización de un Documento Tributario Electrónico (DTE).
/// Representa exactamente un objeto del mensaje JSON de entrada.
/// </summary>
public class Factura
{
    /// <summary>Lugar y fecha: "Guatemala, dd/mm/yyyy hh24:mi hrs."</summary>
    [JsonPropertyName("TIEMPO")]
    public string Tiempo { get; set; } = string.Empty;

    /// <summary>Código interno único enviado por el emisor (máximo 40 posiciones).</summary>
    [JsonPropertyName("REFERENCIA")]
    public string Referencia { get; set; } = string.Empty;

    /// <summary>NIT del emisor de la factura (XXXXXXV).</summary>
    [JsonPropertyName("NIT_EMISOR")]
    public string NitEmisor { get; set; } = string.Empty;

    /// <summary>NIT del receptor de la factura (YYYYYYV).</summary>
    [JsonPropertyName("NIT_RECEPTOR")]
    public string NitReceptor { get; set; } = string.Empty;

    /// <summary>Valor real positivo de 2 posiciones decimales.</summary>
    [JsonPropertyName("VALOR")]
    public decimal Valor { get; set; }

    /// <summary>IVA informado por el emisor.</summary>
    [JsonPropertyName("IVA")]
    public decimal Iva { get; set; }

    /// <summary>Total informado por el emisor (VALOR + IVA).</summary>
    [JsonPropertyName("TOTAL")]
    public decimal Total { get; set; }
}
