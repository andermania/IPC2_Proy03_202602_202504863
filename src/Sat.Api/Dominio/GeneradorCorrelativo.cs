using System.Text.Json;
using Sat.Api.Datos;

namespace Sat.Api.Dominio;

/// <summary>
/// Genera el número único de autorización con estructura yyyymmdd########.
/// El correlativo inicia en 1 cada día y no se repite nunca en ese día: el estado se
/// persiste en un archivo para conservarlo entre reinicios del servicio.
/// La fecha del código corresponde al día en que se solicita la autorización.
/// </summary>
public class GeneradorCorrelativo
{
    private sealed class EstadoCorrelativo
    {
        public string Fecha { get; set; } = string.Empty;
        public long Correlativo { get; set; }
    }

    private readonly string _archivo;
    private readonly object _candado = new();

    public GeneradorCorrelativo(string archivo)
    {
        _archivo = archivo;
    }

    /// <summary>Obtiene el siguiente código de autorización para el día actual.</summary>
    public long ObtenerSiguiente()
    {
        lock (_candado)
        {
            var hoy = DateTime.Now;
            var claveFecha = hoy.ToString("yyyyMMdd");

            var estado = Leer();
            var correlativo = string.Equals(estado.Fecha, claveFecha, StringComparison.Ordinal)
                ? estado.Correlativo + 1
                : 1; // Nuevo día: el correlativo vuelve a iniciar en 1.

            Escribir(new EstadoCorrelativo { Fecha = claveFecha, Correlativo = correlativo });

            // yyyymmdd + correlativo de 8 dígitos precedido con ceros (ej. 1 -> 00000001).
            return long.Parse(
                string.Concat(claveFecha, correlativo.ToString("D8")),
                System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Vacía el estado del correlativo (sistema inicializado).</summary>
    public void Limpiar()
    {
        lock (_candado)
        {
            AlmacenJson.Eliminar(_archivo);
        }
    }

    private EstadoCorrelativo Leer()
    {
        try
        {
            if (File.Exists(_archivo))
            {
                var contenido = File.ReadAllText(_archivo);
                return JsonSerializer.Deserialize<EstadoCorrelativo>(contenido) ?? new EstadoCorrelativo();
            }
        }
        catch (JsonException)
        {
            // Archivo corrupto: se reinicia el correlativo del día.
        }

        return new EstadoCorrelativo();
    }

    private void Escribir(EstadoCorrelativo estado)
    {
        var directorio = Path.GetDirectoryName(_archivo);
        if (!string.IsNullOrEmpty(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var temporal = _archivo + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(estado, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporal, _archivo, overwrite: true);
    }
}
