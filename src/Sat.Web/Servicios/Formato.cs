using System.Globalization;

namespace Sat.Web.Servicios;

/// <summary>Formato de presentación de números en la interfaz.</summary>
public static class Formato
{
    /// <summary>Cultura de Guatemala; si no está disponible se usa la invariante.</summary>
    public static readonly CultureInfo Local = Crear();

    private static CultureInfo Crear()
    {
        try
        {
            return CultureInfo.GetCultureInfo("es-GT");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    /// <summary>Moneda con dos decimales (ej. 1,234.56).</summary>
    public static string Moneda(decimal valor) => valor.ToString("N2", Local);

    /// <summary>Entero separado por miles.</summary>
    public static string Entero(int valor) => valor.ToString("N0", Local);
}
