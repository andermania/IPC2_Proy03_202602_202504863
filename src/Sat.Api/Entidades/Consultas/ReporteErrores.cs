using System.Text.Json.Serialization;

namespace Sat.Api.Entidades.Consultas;

/// <summary>
/// Reporte de facturas recibidas por rango de fechas con el detalle de errores
/// descrito en el enunciado (una fila por cada fecha).
/// </summary>
public class ReporteErrores
{
    [JsonPropertyName("desde")]
    public string? Desde { get; set; }

    [JsonPropertyName("hasta")]
    public string? Hasta { get; set; }

    [JsonPropertyName("facturasSinFecha")]
    public int FacturasSinFecha { get; set; }

    [JsonPropertyName("fechas")]
    public List<ResumenDiarioErrores> Fechas { get; set; } = new();

    [JsonPropertyName("totales")]
    public TotalesReporteErrores Totales { get; set; } = new();
}

/// <summary>Resumen de facturas y errores de una fecha específica.</summary>
public class ResumenDiarioErrores
{
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("cantidadFacturasRecibidas")]
    public int CantidadFacturasRecibidas { get; set; }

    [JsonPropertyName("errores")]
    public ConteoErrores Errores { get; set; } = new();

    [JsonPropertyName("facturasSinErrores")]
    public int FacturasSinErrores { get; set; }

    [JsonPropertyName("emisoresDistintos")]
    public int EmisoresDistintos { get; set; }

    [JsonPropertyName("receptoresDistintos")]
    public int ReceptoresDistintos { get; set; }
}

/// <summary>Totales del reporte para todo el rango de fechas consultado.</summary>
public class TotalesReporteErrores
{
    [JsonPropertyName("cantidadFacturasRecibidas")]
    public int CantidadFacturasRecibidas { get; set; }

    [JsonPropertyName("errores")]
    public ConteoErrores Errores { get; set; } = new();

    [JsonPropertyName("facturasSinErrores")]
    public int FacturasSinErrores { get; set; }

    [JsonPropertyName("emisoresDistintos")]
    public int EmisoresDistintos { get; set; }

    [JsonPropertyName("receptoresDistintos")]
    public int ReceptoresDistintos { get; set; }

    /// <summary>Acumula el resumen de una fecha dentro de los totales.</summary>
    public void Acumular(ResumenDiarioErrores resumen)
    {
        CantidadFacturasRecibidas += resumen.CantidadFacturasRecibidas;
        FacturasSinErrores += resumen.FacturasSinErrores;
        Errores.Acumular(resumen.Errores);
    }
}

/// <summary>Cantidad de errores por categoría (tal como los solicita el enunciado).</summary>
public class ConteoErrores
{
    [JsonPropertyName("nitEmisorInvalido")]
    public int NitEmisorInvalido { get; set; }

    [JsonPropertyName("nitReceptorInvalido")]
    public int NitReceptorInvalido { get; set; }

    [JsonPropertyName("nitEmisorInexistente")]
    public int NitEmisorInexistente { get; set; }

    [JsonPropertyName("nitReceptorInexistente")]
    public int NitReceptorInexistente { get; set; }

    [JsonPropertyName("ivaMalCalculado")]
    public int IvaMalCalculado { get; set; }

    [JsonPropertyName("totalMalCalculado")]
    public int TotalMalCalculado { get; set; }

    [JsonPropertyName("referenciaDuplicada")]
    public int ReferenciaDuplicada { get; set; }

    [JsonPropertyName("referenciaInvalida")]
    public int ReferenciaInvalida { get; set; }

    [JsonPropertyName("fechaInvalida")]
    public int FechaInvalida { get; set; }

    [JsonPropertyName("valorInvalido")]
    public int ValorInvalido { get; set; }

    /// <summary>Suma los contadores de otro conteo.</summary>
    public void Acumular(ConteoErrores otro)
    {
        NitEmisorInvalido += otro.NitEmisorInvalido;
        NitReceptorInvalido += otro.NitReceptorInvalido;
        NitEmisorInexistente += otro.NitEmisorInexistente;
        NitReceptorInexistente += otro.NitReceptorInexistente;
        IvaMalCalculado += otro.IvaMalCalculado;
        TotalMalCalculado += otro.TotalMalCalculado;
        ReferenciaDuplicada += otro.ReferenciaDuplicada;
        ReferenciaInvalida += otro.ReferenciaInvalida;
        FechaInvalida += otro.FechaInvalida;
        ValorInvalido += otro.ValorInvalido;
    }

    ///<summary>Suma un error concreto (usado por el agregador).</summary>
    public void Agregar(string codigoError)
    {
        switch (codigoError)
        {
            case ErroresFactura.NitEmisorInvalido: NitEmisorInvalido++; break;
            case ErroresFactura.NitReceptorInvalido: NitReceptorInvalido++; break;
            case ErroresFactura.NitEmisorInexistente: NitEmisorInexistente++; break;
            case ErroresFactura.NitReceptorInexistente: NitReceptorInexistente++; break;
            case ErroresFactura.IvaIncorrecto: IvaMalCalculado++; break;
            case ErroresFactura.TotalIncorrecto: TotalMalCalculado++; break;
            case ErroresFactura.ReferenciaDuplicada: ReferenciaDuplicada++; break;
            case ErroresFactura.ReferenciaInvalida: ReferenciaInvalida++; break;
            case ErroresFactura.FechaInvalida: FechaInvalida++; break;
            case ErroresFactura.ValorInvalido: ValorInvalido++; break;
        }
    }
}
