using System.Globalization;
using System.Text.RegularExpressions;

namespace Sat.Api.Dominio;

/// <summary>
/// Extracción de la fecha y hora de emisión a partir del campo TIEMPO del mensaje.
/// Se utilizan expresiones regulares para extraer únicamente la información útil
/// (dd/mm/yyyy hh24:mi), descartando el nombre del lugar u otra información que
/// acompañe al texto, la cual NO se considera error.
/// </summary>
public static partial class ExtractorTiempo
{
    /// <summary>
    /// Patron: día/mes/año seguidos de hora y minutos (24 horas).
    /// Se ignoran deliberadamente el nombre del lugar y el texto "hrs.".
    /// </summary>
    [GeneratedRegex(@"(?<!\d)(\d{1,2})/(\d{1,2})/(\d{4})(?!\d)[,\s]+(\d{1,2}):(\d{2})(:\d{2})?", RegexOptions.CultureInvariant)]
    private static partial Regex PatronFechaHora();

    /// <summary>
    /// Extrae la fecha y hora del texto indicado.
    /// Devuelve false solamente cuando la fecha/hora no existe o no es una fecha real de
    /// calendario, o cuando la hora está fuera del rango 0 a 23.
    /// </summary>
    public static bool TryExtraer(string? texto, out DateTime fecha)
    {
        fecha = default;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var coincidencia = PatronFechaHora().Match(texto);
        if (!coincidencia.Success)
        {
            return false;
        }

        var dia = int.Parse(coincidencia.Groups[1].Value, CultureInfo.InvariantCulture);
        var mes = int.Parse(coincidencia.Groups[2].Value, CultureInfo.InvariantCulture);
        var anio = int.Parse(coincidencia.Groups[3].Value, CultureInfo.InvariantCulture);
        var hora = int.Parse(coincidencia.Groups[4].Value, CultureInfo.InvariantCulture);
        var minuto = int.Parse(coincidencia.Groups[5].Value, CultureInfo.InvariantCulture);

        // Validación de calendario y rangos (mes 1-12, día válido, hora 0 a 23).
        if (mes is < 1 or > 12 || dia < 1 || hora > 23 || minuto > 59)
        {
            return false;
        }

        try
        {
            fecha = new DateTime(anio, mes, dia, hora, minuto, 0, DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Día inexistente en el mes (ej. 31/02/2025).
            return false;
        }

        return true;
    }

    /// <summary>Extrae únicamente la fecha (dd/MM/yyyy) del texto indicado.</summary>
    public static bool TryExtraerFecha(string? texto, out DateTime fecha)
    {
        return TryExtraer(texto, out fecha);
    }
}
