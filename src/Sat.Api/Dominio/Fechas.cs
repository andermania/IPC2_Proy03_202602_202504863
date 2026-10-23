using System.Globalization;

namespace Sat.Api.Dominio;

/// <summary>
/// Utilidad para interpretar los parámetros de fecha de las consultas
/// (se acepta dd/MM/yyyy, dd-MM-yyyy o yyyy-MM-dd para facilitar las pruebas
/// desde clientes como Postman).
/// </summary>
public static class Fechas
{
    private static readonly string[] Formatos =
    [
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "yyyy-MM-dd",
        "dd/MM/yyyy HH:mm",
        "yyyy-MM-ddTHH:mm:ss",
    ];

    public static bool TryParse(string? texto, out DateTime fecha)
    {
        fecha = default;

        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        return DateTime.TryParseExact(
            texto.Trim(),
            Formatos,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out fecha);
    }

    public static string Formatear(DateTime fecha) =>
        fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// Interpreta el rango de la consulta. Los valores vacíos usan el mínimo y el
    /// máximo posibles, de modo que no filtran.
    /// </summary>
    public static bool TryParseRango(
        string? desde,
        string? hasta,
        out DateTime fechaDesde,
        out DateTime fechaHasta,
        out string? error)
    {
        fechaDesde = DateTime.MinValue;
        fechaHasta = DateTime.MaxValue;
        error = null;

        if (!string.IsNullOrWhiteSpace(desde))
        {
            if (!TryParse(desde, out fechaDesde))
            {
                error = $"Fecha 'desde' inválida: {desde}. Use el formato dd/MM/yyyy.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(hasta))
        {
            if (!TryParse(hasta, out fechaHasta))
            {
                error = $"Fecha 'hasta' inválida: {hasta}. Use el formato dd/MM/yyyy.";
                return false;
            }
        }

        // Se incluye el día completo en el límite superior.
        if (fechaHasta != DateTime.MaxValue)
        {
            fechaHasta = fechaHasta.AddDays(1).AddTicks(-1);
        }

        if (fechaDesde > fechaHasta)
        {
            error = "El rango de fechas es inválido: 'desde' es posterior a 'hasta'.";
            return false;
        }

        return true;
    }
}
