namespace Sat.Api.Entidades;

/// <summary>
/// Catálogo de errores posibles que pueden presentar las facturas recibidas.
/// Los códigos son los que se almacenan en el registro y se contabilizan en el reporte.
/// </summary>
public static class ErroresFactura
{
    public const string FechaInvalida = "FECHA_INVALIDA";
    public const string ReferenciaInvalida = "REFERENCIA_INVALIDA";
    public const string ReferenciaDuplicada = "REFERENCIA_DUPLICADA";
    public const string NitEmisorInvalido = "NIT_EMISOR_INVALIDO";
    public const string NitReceptorInvalido = "NIT_RECEPTOR_INVALIDO";
    public const string NitEmisorInexistente = "NIT_EMISOR_INEXISTENTE";
    public const string NitReceptorInexistente = "NIT_RECEPTOR_INEXISTENTE";
    public const string ValorInvalido = "VALOR_INVALIDO";
    public const string IvaIncorrecto = "IVA_INCORRECTO";
    public const string TotalIncorrecto = "TOTAL_INCORRECTO";
}
