using System.Text.Json.Serialization;

namespace Sat.Api.Entidades;

/// <summary>Respuesta al mensaje de actualización del RTU.</summary>
public class ResultadoRTU
{
    /// <summary>NITs válidos que no existían y fueron creados.</summary>
    [JsonPropertyName("CONTRIBUYENTES_NUEVOS")]
    public int ContribuyentesNuevos { get; set; }

    /// <summary>NITs válidos que ya existían y cuyo nombre fue actualizado.</summary>
    [JsonPropertyName("CONTRIBUYENTES_ACTUALIZADOS")]
    public int ContribuyentesActualizados { get; set; }

    /// <summary>NITs con dígito verificador o formato inválido (sin efecto sobre el RTU).</summary>
    [JsonPropertyName("NITS_INVALIDOS")]
    public int NitsInvalidos { get; set; }
}
