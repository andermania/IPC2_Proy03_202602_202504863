using System.Text.Json;

namespace Sat.Api.Datos;

/// <summary>
/// Utilidad de persistencia: lectura y escritura de archivos JSON.
/// La escritura es atómica (archivo temporal + renombrado) para evitar corrupción
/// si el servicio se detiene en medio de una operación.
/// </summary>
public static class AlmacenJson
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static T? Leer<T>(string archivo)
    {
        try
        {
            if (!File.Exists(archivo))
            {
                return default;
            }

            var contenido = File.ReadAllText(archivo);
            return JsonSerializer.Deserialize<T>(contenido, Opciones);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public static void Escribir<T>(string archivo, T contenido)
    {
        var directorio = Path.GetDirectoryName(archivo);
        if (!string.IsNullOrEmpty(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var temporal = archivo + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(contenido, Opciones));
        File.Move(temporal, archivo, overwrite: true);
    }

    public static void Eliminar(string archivo)
    {
        if (File.Exists(archivo))
        {
            File.Delete(archivo);
        }
    }
}
