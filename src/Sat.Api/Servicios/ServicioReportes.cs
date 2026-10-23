using System.Globalization;
using Sat.Api.Datos;
using Sat.Api.Dominio;
using Sat.Api.Entidades;
using Sat.Api.Entidades.Consultas;
using Sat.Api.TDA;

namespace Sat.Api.Servicios;

/// <summary>
/// Servicio de reportes y consultas sobre la información persistida:
/// reporte de errores por rango de fechas, consultas de contribuyentes,
/// listados de facturas por NIT, resumen de IVA y resumen de valores por rango.
/// </summary>
public class ServicioReportes
{
    private readonly RepositorioFacturas _facturas;
    private readonly RepositorioContribuyentes _contribuyentes;

    public ServicioReportes(
        RepositorioFacturas facturas,
        RepositorioContribuyentes contribuyentes)
    {
        _facturas = facturas;
        _contribuyentes = contribuyentes;
    }

    /// <summary>Reporte de facturas recibidas y errores para un rango de fechas.</summary>
    public ReporteErrores ReporteErrores(DateTime desde, DateTime hasta)
    {
        var reporte = new ReporteErrores
        {
            Desde = desde == DateTime.MinValue ? null : Fechas.Formatear(desde),
            Hasta = hasta == DateTime.MaxValue ? null : Fechas.Formatear(hasta),
        };

        var resumenes = new TablaHash<string, ResumenDiarioErrores>();
        var ordenFechas = new List<DateTime>();
        var emisoresPorFecha = new Dictionary<string, HashSet<string>>();
        var receptoresPorFecha = new Dictionary<string, HashSet<string>>();
        var todosEmisores = new HashSet<string>();
        var todosReceptores = new HashSet<string>();

        foreach (var registro in _facturas.ObtenerTodos())
        {
            var fecha = FechaEmision(registro);
            if (fecha is null)
            {
                reporte.FacturasSinFecha++;
                continue;
            }

            if (fecha.Value < desde || fecha.Value > hasta)
            {
                continue;
            }

            var clave = Fechas.Formatear(fecha.Value);

            if (!resumenes.IntentarObtener(clave, out var resumen))
            {
                resumen = new ResumenDiarioErrores { Fecha = clave };
                resumenes.Insertar(clave, resumen);
                ordenFechas.Add(fecha.Value);
            }

            resumen.CantidadFacturasRecibidas++;
            if (registro.Aprobada)
            {
                resumen.FacturasSinErrores++;
            }

            foreach (var error in registro.Errores)
            {
                resumen.Errores.Agregar(error);
            }

            AcumularNIT(emisoresPorFecha, clave, registro.NitEmisor);
            AcumularNIT(receptoresPorFecha, clave, registro.NitReceptor);

            var nitEmisor = ValidadorNIT.Normalizar(registro.NitEmisor);
            var nitReceptor = ValidadorNIT.Normalizar(registro.NitReceptor);
            if (nitEmisor.Length > 0)
            {
                todosEmisores.Add(nitEmisor);
            }

            if (nitReceptor.Length > 0)
            {
                todosReceptores.Add(nitReceptor);
            }
        }

        foreach (var fecha in ordenFechas.OrderBy(f => f))
        {
            var clave = Fechas.Formatear(fecha);
            if (!resumenes.IntentarObtener(clave, out var resumen))
            {
                continue;
            }

            resumen.EmisoresDistintos = emisoresPorFecha.TryGetValue(clave, out var emisores) ? emisores.Count : 0;
            resumen.ReceptoresDistintos = receptoresPorFecha.TryGetValue(clave, out var receptores) ? receptores.Count : 0;

            reporte.Fechas.Add(resumen);
            reporte.Totales.Acumular(resumen);
        }

        reporte.Totales.EmisoresDistintos = todosEmisores.Count;
        reporte.Totales.ReceptoresDistintos = todosReceptores.Count;

        return reporte;
    }

    /// <summary>Facturas emitidas o recibidas por un NIT en un rango de fechas, por fecha.</summary>
    public ReporteContribuyente ConsultaContribuyente(string nit, string tipo, DateTime desde, DateTime hasta)
    {
        var normalizado = ValidadorNIT.Normalizar(nit);
        var resultado = new ReporteContribuyente
        {
            Nit = normalizado,
            Nombre = _contribuyentes.Obtener(normalizado)?.Nombre ?? string.Empty,
            Tipo = tipo,
            Desde = desde == DateTime.MinValue ? null : Fechas.Formatear(desde),
            Hasta = hasta == DateTime.MaxValue ? null : Fechas.Formatear(hasta),
            Encontrado = _contribuyentes.Obtener(normalizado) is not null,
        };

        var porFecha = new TablaHash<string, ResumenContribuyenteFecha>();
        var ordenFechas = new List<DateTime>();

        foreach (var registro in Coincidencias(normalizado, tipo, desde, hasta))
        {
            var fecha = FechaEmision(registro);
            if (fecha is null)
            {
                continue;
            }

            var clave = Fechas.Formatear(fecha.Value);
            if (!porFecha.IntentarObtener(clave, out var resumen))
            {
                resumen = new ResumenContribuyenteFecha { Fecha = clave };
                porFecha.Insertar(clave, resumen);
                ordenFechas.Add(fecha.Value);
            }

            AcumularFactura(resumen, registro);
            AcumularFactura(resultado.TotalRango, registro);
        }

        foreach (var fecha in ordenFechas.OrderBy(f => f))
        {
            if (porFecha.IntentarObtener(Fechas.Formatear(fecha), out var resumen))
            {
                resultado.PorFecha.Add(resumen);
            }
        }

        return resultado;
    }

    /// <summary>Listado detallado de facturas emitidas o recibidas por un NIT.</summary>
    public ListadoFacturas ListadoFacturas(string nit, string tipo, DateTime desde, DateTime hasta)
    {
        var normalizado = ValidadorNIT.Normalizar(nit);
        var resultado = new ListadoFacturas
        {
            Nit = normalizado,
            Nombre = _contribuyentes.Obtener(normalizado)?.Nombre ?? string.Empty,
            Tipo = tipo,
            Desde = desde == DateTime.MinValue ? null : Fechas.Formatear(desde),
            Hasta = hasta == DateTime.MaxValue ? null : Fechas.Formatear(hasta),
        };

        foreach (var registro in Coincidencias(normalizado, tipo, desde, hasta))
        {
            resultado.Cantidad++;
            resultado.MontoTotal += registro.Total;
            if (registro.Aprobada)
            {
                resultado.MontoTotalAutorizado += registro.Total;
            }

            resultado.Facturas.Add(new FacturaListado
            {
                Fecha = registro.Fecha,
                Hora = registro.Hora,
                Referencia = registro.Referencia,
                NitEmisor = registro.NitEmisor,
                NitReceptor = registro.NitReceptor,
                Valor = registro.Valor,
                Iva = registro.Iva,
                Total = registro.Total,
                Aprobada = registro.Aprobada,
                CodigoAprobacion = registro.CodigoAprobacion,
                Errores = registro.Errores.AListaNativa(),
            });
        }

        return resultado;
    }

    /// <summary>Resumen de movimientos (IVA emitido y recibido) por NIT en una fecha.</summary>
    public ResumenIVA ResumenIVA(DateTime fecha)
    {
        var clave = Fechas.Formatear(fecha);
        var resultado = new ResumenIVA { Fecha = clave };
        var porNit = new TablaHash<string, MovimientoIVA>();
        var ordenNits = new List<string>();

        foreach (var registro in _facturas.ObtenerTodos())
        {
            var fechaEmision = FechaEmision(registro);
            if (fechaEmision is null || fechaEmision.Value != fecha)
            {
                continue;
            }

            // Emisor: IVA emitido.
            var nitEmisor = ValidadorNIT.Normalizar(registro.NitEmisor);
            var emisor = ObtenerMovimiento(porNit, ordenNits, nitEmisor);
            emisor.FacturasEmitidas++;
            emisor.IvaEmitido += registro.Iva;
            if (registro.Aprobada)
            {
                emisor.IvaEmitidoAutorizado += registro.Iva;
            }

            // Receptor: IVA recibido.
            var nitReceptor = ValidadorNIT.Normalizar(registro.NitReceptor);
            var receptor = ObtenerMovimiento(porNit, ordenNits, nitReceptor);
            receptor.FacturasRecibidas++;
            receptor.IvaRecibido += registro.Iva;
            if (registro.Aprobada)
            {
                receptor.IvaRecibidoAutorizado += registro.Iva;
            }
        }

        foreach (var nit in ordenNits)
        {
            if (!porNit.IntentarObtener(nit, out var movimiento))
            {
                continue;
            }

            resultado.Movimientos.Add(movimiento);
            resultado.Totales.IvaEmitido += movimiento.IvaEmitido;
            resultado.Totales.IvaRecibido += movimiento.IvaRecibido;
            resultado.Totales.IvaEmitidoAutorizado += movimiento.IvaEmitidoAutorizado;
            resultado.Totales.IvaRecibidoAutorizado += movimiento.IvaRecibidoAutorizado;
        }

        return resultado;
    }

    /// <summary>Valor autorizado por fecha en un rango (con IVA o sin IVA).</summary>
    public ResumenRango ResumenRango(DateTime desde, DateTime hasta, bool conIVA)
    {
        var resultado = new ResumenRango
        {
            Desde = desde == DateTime.MinValue ? null : Fechas.Formatear(desde),
            Hasta = hasta == DateTime.MaxValue ? null : Fechas.Formatear(hasta),
            TipoValores = conIVA ? "total" : "sinIva",
        };

        var porFecha = new TablaHash<string, ValorPorFecha>();
        var ordenFechas = new List<DateTime>();

        foreach (var registro in _facturas.ObtenerTodos())
        {
            if (!registro.Aprobada)
            {
                continue; // Solo se consideran facturas autorizadas.
            }

            var fecha = FechaEmision(registro);
            if (fecha is null || fecha.Value < desde || fecha.Value > hasta)
            {
                continue;
            }

            var clave = Fechas.Formatear(fecha.Value);
            if (!porFecha.IntentarObtener(clave, out var valor))
            {
                valor = new ValorPorFecha { Fecha = clave };
                porFecha.Insertar(clave, valor);
                ordenFechas.Add(fecha.Value);
            }

            var monto = conIVA ? registro.Total : registro.Valor;
            valor.ValorAutorizado += monto;
            valor.FacturasAutorizadas++;
            resultado.TotalRango += monto;
        }

        foreach (var fecha in ordenFechas.OrderBy(f => f))
        {
            if (porFecha.IntentarObtener(Fechas.Formatear(fecha), out var valor))
            {
                resultado.PorFecha.Add(valor);
            }
        }

        return resultado;
    }

    /// <summary>Catálogo de contribuyentes del RTU para las consultas.</summary>
    public List<ContribuyenteConsulta> CatalogoNITs()
    {
        var catalogo = new List<ContribuyenteConsulta>();

        foreach (var contribuyente in _contribuyentes.ObtenerTodos())
        {
            catalogo.Add(new ContribuyenteConsulta
            {
                Nit = contribuyente.NIT,
                Nombre = contribuyente.Nombre,
            });
        }

        return catalogo;
    }

    /// <summary>Filtra las facturas por NIT (emitidas, recibidas o ambas) y rango de fechas.</summary>
    private IEnumerable<RegistroFactura> Coincidencias(string nit, string tipo, DateTime desde, DateTime hasta)
    {
        var emitidas = tipo is "emitidas" or "ambas";
        var recibidas = tipo is "recibidas" or "ambas";

        foreach (var registro in _facturas.ObtenerTodos())
        {
            var fecha = FechaEmision(registro);
            if (fecha is null || fecha.Value < desde || fecha.Value > hasta)
            {
                continue;
            }

            var esEmisor = ValidadorNIT.Normalizar(registro.NitEmisor) == nit;
            var esReceptor = ValidadorNIT.Normalizar(registro.NitReceptor) == nit;

            if ((emitidas && esEmisor) || (recibidas && esReceptor))
            {
                yield return registro;
            }
        }
    }

    private static void AcumularFactura(ResumenContribuyenteFecha resumen, RegistroFactura registro)
    {
        resumen.FacturasRecibidas++;
        resumen.MontoRecibido += registro.Total;

        if (registro.Aprobada)
        {
            resumen.FacturasAutorizadas++;
            resumen.MontoAutorizado += registro.Total;
        }
    }

    private static void AcumularNIT(
        Dictionary<string, HashSet<string>> indice,
        string fecha,
        string nit)
    {
        if (!indice.TryGetValue(fecha, out var conjunto))
        {
            conjunto = new HashSet<string>();
            indice[fecha] = conjunto;
        }

        var normalizado = ValidadorNIT.Normalizar(nit);
        if (normalizado.Length > 0)
        {
            conjunto.Add(normalizado);
        }
    }

    private MovimientoIVA ObtenerMovimiento(
        TablaHash<string, MovimientoIVA> indice,
        List<string> orden,
        string nit)
    {
        if (indice.IntentarObtener(nit, out var movimiento))
        {
            return movimiento;
        }

        movimiento = new MovimientoIVA
        {
            Nit = nit,
            Nombre = _contribuyentes.Obtener(nit)?.Nombre ?? string.Empty,
        };

        indice.Insertar(nit, movimiento);
        orden.Add(nit);
        return movimiento;
    }

    private static DateTime? FechaEmision(RegistroFactura registro)
    {
        if (string.IsNullOrWhiteSpace(registro.Fecha))
        {
            return null;
        }

        return DateTime.TryParseExact(
            registro.Fecha,
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var fecha)
            ? fecha
            : null;
    }
}
