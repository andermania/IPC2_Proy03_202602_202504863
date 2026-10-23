namespace Sat.Api.Dominio;

/// <summary>
/// Validación del NIT según el algoritmo descrito en el enunciado del proyecto.
///
/// Formato: XXXXXXV, donde cada X es un dígito (de 1 a 20 posiciones) y V es el
/// dígito verificador (0-9 o K).
///
/// Algoritmo:
///  1. Multiplicar cada carácter por su posición respectiva (posición 1 = carácter más
///     a la derecha), excepto el primero.
///  2. Sumar todos los resultados.
///  3. Obtener el módulo 11 de la sumatoria.
///  4. A 11 restar el resultado del punto 3.
///  5. Calcular el módulo 11 del resultado del punto 4; si es 10, el verificador es K.
/// </summary>
public static class ValidadorNIT
{
    /// <summary>Cantidad máxima de dígitos del cuerpo del NIT (sin el verificador).</summary>
    public const int LongitudMaximaCuerpo = 20;

    /// <summary>
    /// Modo de validación del dígito verificador.
    /// - Enunciado: algoritmo literal descrito en el enunciado (posición 1 = último carácter, sin ciclar).
    /// - SAT: variante oficial del DGI (multiplicadores 2 a 7 ciclando de derecha a izquierda).
    /// - Ambos: se acepta el NIT si cumple cualquiera de los dos algoritmos.
    /// </summary>
    public enum ModoValidacion
    {
        Enunciado,
        SAT,
        Ambos,
    }

    /// <summary>Modo utilizado por el servicio. Se configura en appsettings (ValidacionNIT:Modo).</summary>
    public static ModoValidacion Modo { get; set; } = ModoValidacion.Enunciado;

    /// <summary>
    /// Valida el NIT completo: formato correcto y dígito verificador calculado
    /// según el modo de validación configurado.
    /// </summary>
    public static bool EsValido(string? nit)
    {
        if (!Descomponer(nit, out var cuerpo, out var verificador))
        {
            return false;
        }

        return Modo switch
        {
            ModoValidacion.SAT => CalcularDigitoVerificadorSAT(cuerpo) == verificador,
            ModoValidacion.Ambos =>
                CalcularDigitoVerificador(cuerpo) == verificador ||
                CalcularDigitoVerificadorSAT(cuerpo) == verificador,
            _ => CalcularDigitoVerificador(cuerpo) == verificador,
        };
    }

    /// <summary>Indica únicamente si el NIT tiene formato válido (aún sin verificar el DV).</summary>
    public static bool TieneFormatoValido(string? nit) => Descomponer(nit, out _, out _);

    /// <summary>
    /// Calcula el dígito verificador de un cuerpo de NIT (solo dígitos) aplicando el
    /// algoritmo del enunciado. Devuelve 'K' cuando el resultado es 10.
    /// </summary>
    public static char CalcularDigitoVerificador(string cuerpo)
    {
        long sumatoria = 0;

        // Recorre de derecha a izquierda (posición 1 = último carácter),
        // omitiendo el primer carácter (el más a la izquierda).
        var posicion = 1;
        for (var i = cuerpo.Length - 1; i >= 1; i--, posicion++)
        {
            sumatoria += (cuerpo[i] - '0') * posicion;
        }

        var modulo = (int)(sumatoria % 11);
        var resta = 11 - modulo;
        var resultado = resta % 11;

        return resultado == 10 ? 'K' : (char)('0' + resultado);
    }

    /// <summary>
    /// Variante oficial del DGI: multiplica cada dígito del cuerpo por multiplicadores
    /// que ciclan de 2 a 7, de derecha a izquierda; (11 - (suma mod 11)) mod 11,
    /// con 10 = K y 11 = 0.
    /// </summary>
    public static char CalcularDigitoVerificadorSAT(string cuerpo)
    {
        long sumatoria = 0;
        var multiplicador = 2;

        for (var i = cuerpo.Length - 1; i >= 0; i--, multiplicador++)
        {
            if (multiplicador > 7)
            {
                multiplicador = 2;
            }

            sumatoria += (cuerpo[i] - '0') * multiplicador;
        }

        var resta = 11 - (int)(sumatoria % 11);
        if (resta == 11)
        {
            return '0';
        }

        return resta == 10 ? 'K' : (char)('0' + resta);
    }

    /// <summary>
    /// Separa el NIT en cuerpo (dígitos) y dígito verificador.
    /// Se acepta el separador opcional '-' entre el cuerpo y el verificador (ej. 7378106-4).
    /// </summary>
    private static bool Descomponer(string? nit, out string cuerpo, out char verificador)
    {
        cuerpo = string.Empty;
        verificador = '\0';

        if (string.IsNullOrWhiteSpace(nit))
        {
            return false;
        }

        var texto = nit.Trim().ToUpperInvariant();

        // Separa el dígito verificador (último carácter).
        if (texto.Length < 2)
        {
            return false;
        }

        verificador = texto[^1];
        var sinVerificador = texto[..^1];

        // El verificador debe ser un dígito o K.
        if (verificador == 'K')
        {
            // Válido: el verificador es K.
        }
        else if (verificador is < '0' or > '9')
        {
            return false;
        }

        // Opcional: separador '-' entre cuerpo y verificador (7378106-4).
        if (sinVerificador.EndsWith('-'))
        {
            sinVerificador = sinVerificador[..^1];
        }

        if (sinVerificador.Length is < 1 or > LongitudMaximaCuerpo)
        {
            return false;
        }

        // El cuerpo debe contener exclusivamente dígitos.
        foreach (var caracter in sinVerificador)
        {
            if (caracter is < '0' or > '9')
            {
                return false;
            }
        }

        cuerpo = sinVerificador;
        return true;
    }

    /// <summary>Normaliza un NIT para indexarlo en el RTU (sin separador, sin espacios, mayúsculas).</summary>
    public static string Normalizar(string? nit)
    {
        if (string.IsNullOrWhiteSpace(nit))
        {
            return string.Empty;
        }

        return nit.Trim().Replace("-", string.Empty).ToUpperInvariant();
    }
}
