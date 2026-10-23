using System.Text.Json;

namespace Sat.Api.Infraestructura;

/// <summary>
/// Lectura de los mensajes JSON de entrada. Acepta tanto un objeto único como un
/// arreglo de objetos, para poder consumir los mensajes descritos en el enunciado.
/// </summary>
public static class Mensajes
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>
    /// Deserializa el cuerpo del mensaje en una lista de elementos.
    /// Devuelve false si el mensaje no tiene la estructura esperada.
    /// </summary>
    public static bool Deserializar<T>(JsonElement cuerpo, out List<T> elementos)
    {
        elementos = new List<T>();

        switch (cuerpo.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var elemento in cuerpo.EnumerateArray())
                {
                    if (!DeserializarElemento<T>(elemento, out var item))
                    {
                        return false;
                    }

                    elementos.Add(item!);
                }

                return elementos.Count > 0;

            case JsonValueKind.Object:
                if (!DeserializarElemento<T>(cuerpo, out var unico))
                {
                    return false;
                }

                elementos.Add(unico!);
                return true;

            default:
                return false;
        }
    }

    private static bool DeserializarElemento<T>(JsonElement elemento, out T? resultado)
    {
        resultado = default;

        try
        {
            resultado = elemento.Deserialize<T>(Opciones);
        }
        catch (JsonException)
        {
            return false;
        }

        return resultado is not null;
    }
}
