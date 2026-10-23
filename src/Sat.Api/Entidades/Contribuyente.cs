using System.Text.Json.Serialization;

namespace Sat.Api.Entidades;

/// <summary>
/// Contribuyente registrado en el Registro Tributario Unificado (RTU).
/// Se utiliza tanto para el mensaje de entrada de actualización del RTU
/// como para la persistencia en disco.
/// </summary>
public class Contribuyente
{
    /// <summary>NIT del contribuyente, incluye el dígito verificador.</summary>
    [JsonPropertyName("NIT")]
    public string NIT { get; set; } = string.Empty;

    /// <summary>Razón social o nombre del contribuyente.</summary>
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;
}
