using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sat.Api.Infraestructura;

/// <summary>
/// Lectura de los mensajes JSON de entrada.
///
/// El enunciado define los mensajes de solicitud como un arreglo de objetos, por lo
/// que el servicio exige exactamente esa estructura: un arreglo con uno o más objetos
/// que contengan al menos una de las propiedades del mensaje. Cualquier otra forma se
/// rechaza con un mensaje de error en español.
/// </summary>
public static class Mensajes
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>
    /// Lee el cuerpo de la petición y lo interpreta como JSON.
    ///
    /// Se lee manualmente para poder responder con un mensaje en español cuando el
    /// archivo está vacío o su sintaxis está rota (por ejemplo, cortado a la mitad);
    /// si la lectura la hiciera el framework, el cliente recibiría el error
    /// genérico en inglés del serializador y no un mensaje claro.
    /// Devuelve null cuando el cuerpo no es JSON válido.
    /// </summary>
    public static async Task<JsonElement?> LeerCuerpoAsync(HttpRequest peticion)
    {
        string texto;

        using (var lector = new StreamReader(peticion.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
        {
            texto = await lector.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        try
        {
            using var documento = JsonDocument.Parse(texto);
            return documento.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Propiedades del mensaje de actualización del RTU.</summary>
    public static readonly string[] PropiedadesRTU = ["NIT", "nombre"];

    /// <summary>Propiedades del mensaje de solicitud de autorización.</summary>
    public static readonly string[] PropiedadesAutorizacion =
        ["TIEMPO", "REFERENCIA", "NIT_EMISOR", "NIT_RECEPTOR", "VALOR", "IVA", "TOTAL"];

    /// <summary>
    /// Deserializa el cuerpo del mensaje (arreglo de objetos) en una lista de elementos.
    /// Devuelve false si el mensaje no tiene la estructura esperada.
    /// </summary>
    public static bool Deserializar<T>(JsonElement cuerpo, string[] propiedades, out List<T> elementos)
    {
        elementos = new List<T>();

        // Los mensajes de solicitud son, según el enunciado, un arreglo de objetos.
        if (cuerpo.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var elemento in cuerpo.EnumerateArray())
        {
            if (!TienePropiedadesReconocidas(elemento, propiedades))
            {
                return false;
            }

            if (!DeserializarElemento<T>(elemento, out var item))
            {
                return false;
            }

            elementos.Add(item!);
        }

        // Un arreglo vacío es un mensaje bien formado que no contiene solicitudes:
        // se procesa sin errores y con todos los contadores en cero.
        return true;
    }

    /// <summary>Verifica que el objeto contenga al menos una de las propiedades del mensaje.</summary>
    private static bool TienePropiedadesReconocidas(JsonElement elemento, string[] propiedades)
    {
        if (elemento.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var propiedad in elemento.EnumerateObject())
        {
            foreach (var esperada in propiedades)
            {
                if (string.Equals(propiedad.Name, esperada, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
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
