using System.Globalization;
using System.Text;

namespace Sat.Web.Servicios;

/// <summary>
/// Generador de documentos PDF escrito sobre el formato PDF 1.4, sin utilizar
/// librerías de terceros. Permite emitir títulos, párrafos, líneas y tablas con
/// salto de página automático, lo que permite exportar todos los reportes del
/// servicio a un único documento.
/// </summary>
public sealed class GeneradorPdf
{
    /// <summary>Tamaño de la página en puntos (A4: 210 x 297 mm).</summary>
    private const double AnchoPagina = 595.28;
    private const double AltoPagina = 841.89;

    private const double MargenIzquierdo = 46;
    private const double MargenDerecho = 46;
    private const double MargenSuperior = 52;
    private const double MargenInferior = 52;

    private const double AnchoUtil = AnchoPagina - MargenIzquierdo - MargenDerecho;

    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    private readonly List<string> _contenidos = new();
    private StringBuilder _actual = new();
    private double _cursorY;
    private bool _hayPagina;

    public GeneradorPdf()
    {
        NuevaPagina();
    }

    /// <summary>Cantidad de páginas emitidas hasta el momento.</summary>
    public int Paginas => _contenidos.Count;

    // -------------------------------------------------------------------------
    // Composición de contenido
    // -------------------------------------------------------------------------

    /// <summary>Título principal del documento (negrita 16).</summary>
    public void Titulo(string texto)
    {
        AsegurarEspacio(30);
        TextoInterno(texto, 16, negrita: true, MargenIzquierdo);
        _cursorY += 6;
        Linea();
        _cursorY += 12;
    }

    /// <summary>Título de sección (negrita 12).</summary>
    public void Subtitulo(string texto)
    {
        AsegurarEspacio(24);
        _cursorY += 8;
        TextoInterno(texto, 12, negrita: true, MargenIzquierdo);
        _cursorY += 8;
    }

    /// <summary>Párrafo normal (10), con salto de línea automático.</summary>
    public void Parrafo(string texto)
    {
        foreach (var linea in Envolver(texto, AnchoUtil, 10, negrita: false))
        {
            AsegurarEspacio(14);
            TextoInterno(linea, 10, negrita: false, MargenIzquierdo);
            _cursorY += 13;
        }
    }

    /// <summary>Par etiqueta / valor en dos columnas.</summary>
    public void Par(string etiqueta, string valor)
    {
        AsegurarEspacio(14);
        TextoInterno(etiqueta + ":", 10, negrita: true, MargenIzquierdo);
        TextoInterno(valor, 10, negrita: false, MargenIzquierdo + 190);
        _cursorY += 14;
    }

    /// <summary>Línea horizontal separadora.</summary>
    public void Linea()
    {
        AsegurarEspacio(8);
        _cursorY += 4;
        var y = ToY(_cursorY);
        _actual.Append(Invariante, $"0.72 0.76 0.81 RG 0.6 w {F(MargenIzquierdo)} {F(y)} m {F(AnchoPagina - MargenDerecho)} {F(y)} l S\n");
        _cursorY += 4;
    }

    /// <summary>Espacio en blanco vertical.</summary>
    public void Espacio(double puntos)
    {
        AsegurarEspacio(puntos);
        _cursorY += puntos;
    }

    /// <summary>
    /// Tabla con encabezado, bordes y salto de página automático (vuelve a
    /// imprimir el encabezado cuando continúa en una página nueva).
    /// </summary>
    public void Tabla(string[] encabezados, double[] anchos, IReadOnlyList<string[]> filas)
    {
        if (encabezados.Length != anchos.Length)
        {
            throw new ArgumentException("La cantidad de encabezados y de anchos debe coincidir.");
        }

        var x = new double[encabezados.Length];
        var acumulado = 0.0;
        for (var i = 0; i < anchos.Length; i++)
        {
            x[i] = MargenIzquierdo + AnchoUtil * acumulado;
            acumulado += anchos[i];
        }

        const double altoEncabezado = 17;
        const double altoFila = 15;

        DibujarFilaTabla(x, anchos, encabezados, altoEncabezado, encabezado: true);

        foreach (var fila in filas)
        {
            if (_cursorY + altoFila > AltoPagina - MargenInferior)
            {
                NuevaPagina();
                DibujarFilaTabla(x, anchos, encabezados, altoEncabezado, encabezado: true);
            }

            DibujarFilaTabla(x, anchos, fila, altoFila, encabezado: false);
        }

        _cursorY += 8;
    }

    private void DibujarFilaTabla(
        double[] x,
        double[] anchos,
        string[] celdas,
        double alto,
        bool encabezado)
    {
        var yArriba = _cursorY;
        var y = ToY(yArriba);

        // Fondo de la fila.
        var fondo = encabezado ? "0.90 0.93 0.96" : "1 1 1";
        _actual.Append(Invariante,
            $"{fondo} rg {F(MargenIzquierdo)} {F(y - alto)} {F(AnchoUtil)} {F(alto)} re f\n");
        _actual.Append("0 0 0 rg\n");

        // Texto de cada celda.
        for (var i = 0; i < celdas.Length && i < x.Length; i++)
        {
            var anchoCelda = AnchoUtil * anchos[i] - 8;
            var texto = Truncar(celdas[i], anchoCelda, encabezado ? 9 : 9, !encabezado);
            var tamanio = encabezado ? 9 : 9;

            TextoInterno(
                texto,
                tamanio,
                negrita: encabezado,
                x[i] + 4,
                yArriba - (alto - tamanio) / 2 - 1);
        }

        // Bordes inferiores de la fila.
        _cursorY += alto;
        var yBase = ToY(_cursorY);
        _actual.Append(Invariante,
            $"0.72 0.76 0.81 RG 0.5 w {F(MargenIzquierdo)} {F(yBase)} m {F(AnchoPagina - MargenDerecho)} {F(yBase)} l S\n");
    }

    // -------------------------------------------------------------------------
    // Página
    // -------------------------------------------------------------------------

    private void NuevaPagina()
    {
        if (_hayPagina)
        {
            _contenidos.Add(_actual.ToString());
        }

        _actual = new StringBuilder();
        _hayPagina = true;
        _cursorY = MargenSuperior;
    }

    private void AsegurarEspacio(double alto)
    {
        if (_cursorY + alto > AltoPagina - MargenInferior)
        {
            NuevaPagina();
        }
    }

    private void TextoInterno(string texto, double tamano, bool negrita, double x, double? yAbsoluto = null)
    {
        var y = ToY(yAbsoluto ?? _cursorY);
        var fuente = negrita ? "/F2" : "/F1";

        _actual.Append("BT\n");
        _actual.Append(Invariante, $"{fuente} {F(tamano)} Tf\n");
        _actual.Append(Invariante, $"1 0 0 1 {F(x)} {F(y)} Tm\n");
        _actual.Append("(").Append(Escapar(Codificar(texto))).Append(") Tj\n");
        _actual.Append("ET\n");
    }

    private static double ToY(double desdeArriba) => AltoPagina - desdeArriba;

    // -------------------------------------------------------------------------
    // Construcción del archivo PDF
    // -------------------------------------------------------------------------

    /// <summary>Construye el archivo PDF completo y devuelve sus bytes.</summary>
    public byte[] Construir()
    {
        if (_hayPagina)
        {
            _contenidos.Add(_actual.ToString());
            _actual = new StringBuilder();
            _hayPagina = false;
        }

        var totalPaginas = _contenidos.Count;

        // Orden de objetos: 1 catálogo, 2 páginas, 3-4 fuentes, luego página/contenido.
        var objetos = new List<string>();
        var idsPagina = new List<int>();

        for (var i = 0; i < totalPaginas; i++)
        {
            idsPagina.Add(5 + i * 2);
        }

        var hijos = string.Join(" ", idsPagina.Select(id => $"{id} 0 R"));
        objetos.Add($"<< /Type /Catalog /Pages 2 0 R >>");
        objetos.Add($"<< /Type /Pages /Kids [{hijos}] /Count {totalPaginas} >>");
        objetos.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        objetos.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

        for (var i = 0; i < totalPaginas; i++)
        {
            var idContenido = 6 + i * 2;
            objetos.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(AnchoPagina)} {F(AltoPagina)}] " +
                "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> " +
                $"/Contents {idContenido} 0 R >>");
        }

        foreach (var contenido in _contenidos)
        {
            // Latin1 conserva byte a byte el texto ya codificado a WinAnsi.
            var bytes = Encoding.Latin1.GetBytes(contenido);
            objetos.Add($"<< /Length {bytes.Length} >>\nstream\n{contenido}endstream");
        }

        using var salida = new MemoryStream();
        var encabezado = Encoding.Latin1.GetBytes("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        salida.Write(encabezado, 0, encabezado.Length);

        var offsets = new List<long>();

        for (var i = 0; i < objetos.Count; i++)
        {
            offsets.Add(salida.Length);
            var cuerpo = Encoding.Latin1.GetBytes($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
            salida.Write(cuerpo, 0, cuerpo.Length);
        }

        var inicioXref = salida.Length;
        var xref = new StringBuilder();
        xref.Append("xref\n");
        xref.Append($"0 {objetos.Count + 1}\n");
        xref.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            xref.Append(offset.ToString("0000000000", Invariante)).Append(" 00000 n \n");
        }

        xref.Append("trailer\n");
        xref.Append($"<< /Size {objetos.Count + 1} /Root 1 0 R >>\n");
        xref.Append("startxref\n");
        xref.Append(inicioXref.ToString(Invariante)).Append("\n%%EOF\n");

        var bytesXref = Encoding.Latin1.GetBytes(xref.ToString());
        salida.Write(bytesXref, 0, bytesXref.Length);

        return salida.ToArray();
    }

    // -------------------------------------------------------------------------
    // Utilidades de texto
    // -------------------------------------------------------------------------

    /// <summary>Divide un texto en líneas que caben en el ancho indicado.</summary>
    private static List<string> Envolver(string texto, double ancho, double tamano, bool negrita)
    {
        var palabras = (texto ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lineas = new List<string>();
        var actual = new StringBuilder();

        foreach (var palabra in palabras)
        {
            var propuesta = actual.Length == 0 ? palabra : actual + " " + palabra;

            if (AnchoTexto(propuesta.Length, tamano, negrita) > ancho && actual.Length > 0)
            {
                lineas.Add(actual.ToString());
                actual.Clear();
                actual.Append(palabra);
            }
            else
            {
                actual.Clear();
                actual.Append(propuesta);
            }
        }

        if (actual.Length > 0)
        {
            lineas.Add(actual.ToString());
        }

        return lineas.Count == 0 ? new List<string> { string.Empty } : lineas;
    }

    /// <summary>Estima el ancho de un texto en puntos (Helvetica).</summary>
    private static double AnchoTexto(int caracteres, double tamano, bool negrita)
        => caracteres * tamano * (negrita ? 0.55 : 0.50);

    /// <summary>Recorta un texto para que quepa en el ancho indicado.</summary>
    private static string Truncar(string texto, double ancho, double tamano, bool negrita)
    {
        texto ??= string.Empty;

        if (AnchoTexto(texto.Length, tamano, negrita) <= ancho)
        {
            return texto;
        }

        var maximo = Math.Max(1, (int)(ancho / (tamano * (negrita ? 0.55 : 0.50))) - 1);
        return texto.Length <= maximo ? texto : texto[..maximo] + "…";
    }

    private static string F(double valor) => valor.ToString("0.##", Invariante);

    private static string Escapar(byte[] bytes)
    {
        var texto = Encoding.Latin1.GetString(bytes);
        var salida = new StringBuilder(texto.Length);

        foreach (var caracter in texto)
        {
            switch (caracter)
            {
                case '\\':
                    salida.Append("\\\\");
                    break;
                case '(':
                    salida.Append("\\(");
                    break;
                case ')':
                    salida.Append("\\)");
                    break;
                default:
                    salida.Append(caracter);
                    break;
            }
        }

        return salida.ToString();
    }

    /// <summary>
    /// Codifica a WinAnsi (CP1252), la codificación de los tipos de letra
    /// estándar de PDF: conserva los acentos y la ñ del español.
    /// Se normaliza a forma compuesta (NFC) para que las letras con tilde
    /// combinante no se pierdan.
    /// </summary>
    private static byte[] Codificar(string texto)
    {
        texto ??= string.Empty;
        texto = texto.Normalize(NormalizationForm.FormC);
        var salida = new byte[texto.Length];

        for (var i = 0; i < texto.Length; i++)
        {
            var c = texto[i];
            salida[i] = c switch
            {
                <= '\u00FF' => (byte)c,
                '\u2018' => 0x91, // ‘
                '\u2019' => 0x92, // ’
                '\u201C' => 0x93, // “
                '\u201D' => 0x94, // ”
                '\u2013' => 0x96, // –
                '\u2014' => 0x97, // —
                '\u2026' => 0x85, // …
                '\u2022' => 0x95, // •
                _ => (byte)'?',
            };
        }

        return salida;
    }
}
