using System.Globalization;
using System.Text;

namespace Sat.Web.Servicios;

/// <summary>
/// Generación de gráficas de barras en SVG desde el servidor.
/// No depende de librerías externas ni de acceso a Internet, por lo que las
/// gráficas se despliegan igual en cualquier equipo.
/// </summary>
public static class GraficaSvg
{
    private const double Ancho = 820;
    private const double Alto = 360;
    private const double MargenIzquierdo = 78;
    private const double MargenDerecho = 24;
    private const double MargenSuperior = 52;
    private const double MargenInferior = 74;

    private const string ColorFondo = "#ffffff";
    private const string ColorRejilla = "#d7dde5";
    private const string ColorEjes = "#6b7785";
    private const string ColorTexto = "#22303f";
    private const string ColorEmitido = "#2d5a9e";
    private const string ColorRecibido = "#e08b2c";
    private const string ColorRango = "#1f3a5f";

    /// <summary>Gráfica de una sola serie (resumen por rango de fechas).</summary>
    public static string Barras(
        string titulo,
        IReadOnlyList<string> etiquetas,
        IReadOnlyList<decimal> valores,
        string leyenda)
    {
        return Construir(titulo, etiquetas, new[] { (leyenda, valores, ColorRango) }, esAgrupada: false);
    }

    /// <summary>Gráfica de dos series por categoría (resumen de IVA emitido y recibido).</summary>
    public static string BarrasAgrupadas(
        string titulo,
        IReadOnlyList<string> etiquetas,
        IReadOnlyList<decimal> emitido,
        IReadOnlyList<decimal> recibido)
    {
        return Construir(
            titulo,
            etiquetas,
            new[]
            {
                ("IVA emitido", emitido, ColorEmitido),
                ("IVA recibido", recibido, ColorRecibido),
            },
            esAgrupada: true);
    }

    private static string Construir(
        string titulo,
        IReadOnlyList<string> etiquetas,
        (string Leyenda, IReadOnlyList<decimal> Valores, string Color)[] series,
        bool esAgrupada)
    {
        var svg = new StringBuilder();
        var plotIzquierda = MargenIzquierdo;
        var plotDerecha = Ancho - MargenDerecho;
        var plotArriba = MargenSuperior;
        var plotAbajo = Alto - MargenInferior;
        var anchoPlot = plotDerecha - plotIzquierda;
        var altoPlot = plotAbajo - plotArriba;

        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Ancho} {Alto}\" width=\"100%\" role=\"img\" aria-label=\"{Escapar(titulo)}\">");
        svg.Append(CultureInfo.InvariantCulture, $"<rect width=\"{Ancho}\" height=\"{Alto}\" fill=\"{ColorFondo}\" />");

        // Título.
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Ancho / 2}\" y=\"26\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"16\" font-weight=\"700\" fill=\"{ColorTexto}\">{Escapar(titulo)}</text>");

        if (etiquetas.Count == 0)
        {
            svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{Ancho / 2}\" y=\"{plotArriba + altoPlot / 2}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"14\" fill=\"{ColorEjes}\">Sin información para mostrar en el rango seleccionado.</text>");
            svg.Append("</svg>");
            return svg.ToString();
        }

        var maximo = Maximo(series);
        var techo = TechoAdecuado(maximo);
        var divisiones = 5;

        // Rejilla y escala del eje Y.
        for (var i = 0; i <= divisiones; i++)
        {
            var valor = techo * i / divisiones;
            var y = plotAbajo - (altoPlot * i / divisiones);

            svg.Append(CultureInfo.InvariantCulture,
                $"<line x1=\"{plotIzquierda}\" y1=\"{F(y)}\" x2=\"{plotDerecha}\" y2=\"{F(y)}\" stroke=\"{ColorRejilla}\" stroke-width=\"1\" />");
            svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{F(plotIzquierda - 8)}\" y=\"{F(y + 4)}\" text-anchor=\"end\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"11\" fill=\"{ColorEjes}\">{Escapar(Formato(valor))}</text>");
        }

        // Ejes.
        svg.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{plotIzquierda}\" y1=\"{plotArriba}\" x2=\"{plotIzquierda}\" y2=\"{plotAbajo}\" stroke=\"{ColorEjes}\" stroke-width=\"1.5\" />");
        svg.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{plotIzquierda}\" y1=\"{plotAbajo}\" x2=\"{plotDerecha}\" y2=\"{plotAbajo}\" stroke=\"{ColorEjes}\" stroke-width=\"1.5\" />");

        var cantidadCategorias = etiquetas.Count;
        var anchoCategoria = anchoPlot / cantidadCategorias;
        var separacion = Math.Min(14, anchoCategoria * 0.25);
        var anchoDisponible = anchoCategoria - separacion;
        var anchoBarra = esAgrupada
            ? Math.Max(4, (anchoDisponible - 4) / series.Length)
            : anchoDisponible;

        for (var c = 0; c < cantidadCategorias; c++)
        {
            var centro = plotIzquierda + anchoCategoria * (c + 0.5);

            for (var s = 0; s < series.Length; s++)
            {
                var valor = series[s].Valores[c];
                var altoBarra = techo == 0 ? 0 : (double)(valor / techo) * altoPlot;
                var x = centro - (anchoBarra * series.Length + (series.Length - 1) * 4) / 2
                    + s * (anchoBarra + 4);
                var y = plotAbajo - altoBarra;

                if (altoBarra > 0)
                {
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(anchoBarra)}\" height=\"{F(altoBarra)}\" fill=\"{series[s].Color}\" rx=\"2\"><title>{Escapar(etiquetas[c])} - {Escapar(series[s].Leyenda)}: {Escapar(Formato(valor))}</title></rect>");
                }
            }

            // Etiqueta del eje X.
            svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{F(centro)}\" y=\"{F(plotAbajo + 18)}\" text-anchor=\"middle\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"11\" fill=\"{ColorTexto}\">{Escapar(Truncar(etiquetas[c], 16))}</text>");
        }

        // Leyenda.
        var xLeyenda = plotIzquierda;
        var yLeyenda = Alto - 26;
        foreach (var serie in series)
        {
            svg.Append(CultureInfo.InvariantCulture,
                $"<rect x=\"{F(xLeyenda)}\" y=\"{F(yLeyenda - 10)}\" width=\"14\" height=\"14\" rx=\"2\" fill=\"{serie.Color}\" />");
            svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{F(xLeyenda + 20)}\" y=\"{F(yLeyenda + 1)}\" font-family=\"Segoe UI, Arial, sans-serif\" font-size=\"12\" fill=\"{ColorTexto}\">{Escapar(serie.Leyenda)}</text>");
            xLeyenda += 30 + serie.Leyenda.Length * 7.5;
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    private static decimal Maximo(
        (string Leyenda, IReadOnlyList<decimal> Valores, string Color)[] series)
    {
        decimal maximo = 0;

        foreach (var serie in series)
        {
            foreach (var valor in serie.Valores)
            {
                if (valor > maximo)
                {
                    maximo = valor;
                }
            }
        }

        return maximo;
    }

    /// <summary>Redondea el máximo hacia arriba a un valor "bonito" para el eje.</summary>
    private static decimal TechoAdecuado(decimal maximo)
    {
        if (maximo <= 0)
        {
            return 1;
        }

        var exponente = (int)Math.Floor(Math.Log10((double)maximo));
        var baseDiez = (decimal)Math.Pow(10, exponente);
        var fraccion = maximo / baseDiez;

        var bonito = fraccion switch
        {
            <= 1 => 1m,
            <= 2 => 2m,
            <= 2.5m => 2.5m,
            <= 5 => 5m,
            _ => 10m,
        };

        var techo = bonito * baseDiez;

        return techo < maximo ? techo * 2 : techo;
    }

    private static string Formato(decimal valor)
        => valor.ToString("N2", CultureInfo.InvariantCulture);

    private static string F(double valor)
        => valor.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Truncar(string texto, int maximo)
        => texto.Length <= maximo ? texto : texto[..(maximo - 1)] + "…";

    private static string Escapar(string texto)
    {
        return texto
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
