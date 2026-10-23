using System.Globalization;
using Sat.Api.Datos;
using Sat.Api.Dominio;
using Sat.Api.Entidades;
using Sat.Api.TDA;

namespace Sat.Api.Servicios;

/// <summary>
/// Procesa las solicitudes de autorización de facturas: aplica todas las reglas del
/// enunciado, asigna el código de autorización correlativo a las facturas sin errores,
/// registra los errores para el reporte y persiste la información recibida.
/// </summary>
public class ProcesadorAutorizaciones
{
    private readonly ServicioRTU _rtu;
    private readonly RepositorioFacturas _facturas;
    private readonly GeneradorCorrelativo _correlativo;

    public ProcesadorAutorizaciones(
        ServicioRTU rtu,
        RepositorioFacturas facturas,
        GeneradorCorrelativo correlativo)
    {
        _rtu = rtu;
        _facturas = facturas;
        _correlativo = correlativo;
    }

    public ResultadoAutorizaciones Procesar(IList<Factura> solicitudes)
    {
        var detalle = new ResultadoAutorizacionesDetalle();
        var registrosNuevos = new ListaEnlazada<RegistroFactura>();

        foreach (var factura in solicitudes)
        {
            var registro = CrearRegistro(factura);
            var errores = registro.Errores;

            ValidarFecha(factura, registro);
            ValidarReferencia(factura, registro);
            ValidarContribuyente(
                factura.NitEmisor,
                ErroresFactura.NitEmisorInvalido,
                ErroresFactura.NitEmisorInexistente,
                errores);
            ValidarContribuyente(
                factura.NitReceptor,
                ErroresFactura.NitReceptorInvalido,
                ErroresFactura.NitReceptorInexistente,
                errores);
            ValidarMontos(factura, errores);

            var sinErrores = errores.EstaVacia;

            if (sinErrores)
            {
                registro.Aprobada = true;
                registro.CodigoAprobacion = _correlativo.ObtenerSiguiente();
                detalle.DetalleAprobaciones.Add(new DetalleAprobacion
                {
                    Referencia = registro.Referencia,
                    CodigoAprobacion = registro.CodigoAprobacion.Value,
                });
                detalle.CantidadAutorizacionesAprobadas++;
            }
            else
            {
                ContabilizarRechazos(errores, detalle);
            }

            registrosNuevos.Agregar(registro);
        }

        // Persiste los registros (incluye las referencias ya usadas para futuras
        // solicitudes: la primera referencia gana).
        _facturas.Agregar(registrosNuevos);

        return new ResultadoAutorizaciones { Detalle = detalle };
    }

    private static RegistroFactura CrearRegistro(Factura factura)
    {
        return new RegistroFactura
        {
            Tiempo = factura.Tiempo ?? string.Empty,
            Referencia = factura.Referencia ?? string.Empty,
            NitEmisor = factura.NitEmisor ?? string.Empty,
            NitReceptor = factura.NitReceptor ?? string.Empty,
            Valor = factura.Valor,
            Iva = factura.Iva,
            Total = factura.Total,
            FechaProceso = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        };
    }

    private static void ValidarFecha(Factura factura, RegistroFactura registro)
    {
        // Solo se extrae la información útil de TIEMPO; el nombre del lugar no es error.
        if (ExtractorTiempo.TryExtraer(factura.Tiempo, out var fecha))
        {
            registro.Fecha = fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            registro.Hora = fecha.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
        else
        {
            registro.Errores.Agregar(ErroresFactura.FechaInvalida);
        }
    }

    private void ValidarReferencia(Factura factura, RegistroFactura registro)
    {
        var errores = registro.Errores;
        var referencia = factura.Referencia?.Trim() ?? string.Empty;

        // Referencia: texto único de máximo 40 posiciones.
        if (referencia.Length is < 1 or > 40)
        {
            errores.Agregar(ErroresFactura.ReferenciaInvalida);
            return;
        }

        registro.Referencia = referencia;

        // Índice global de referencias: la primera referencia gana.
        if (_facturas.ExisteReferencia(referencia))
        {
            errores.Agregar(ErroresFactura.ReferenciaDuplicada);
        }
        else
        {
            _facturas.ReservarReferencia(referencia);
        }
    }

    private void ValidarContribuyente(
        string? nit,
        string errorFormato,
        string errorInexistente,
        ListaEnlazada<string> errores)
    {
        if (!ValidadorNIT.EsValido(nit))
        {
            errores.Agregar(errorFormato);
            return;
        }

        if (!_rtu.Existe(nit))
        {
            errores.Agregar(errorInexistente);
        }
    }

    private static void ValidarMontos(Factura factura, ListaEnlazada<string> errores)
    {
        // VALOR: valor real positivo de 2 posiciones decimales.
        if (factura.Valor <= 0 || decimal.Round(factura.Valor, 2) != factura.Valor)
        {
            errores.Agregar(ErroresFactura.ValorInvalido);
        }

        // IVA: REDONDEAR(VALOR * 0.12, 2).
        var ivaEsperado = decimal.Round(factura.Valor * 0.12m, 2, MidpointRounding.AwayFromZero);
        if (factura.Iva != ivaEsperado)
        {
            errores.Agregar(ErroresFactura.IvaIncorrecto);
        }

        // TOTAL: VALOR + IVA.
        if (factura.Total != factura.Valor + factura.Iva)
        {
            errores.Agregar(ErroresFactura.TotalIncorrecto);
        }
    }

    private static void ContabilizarRechazos(
        ListaEnlazada<string> errores,
        ResultadoAutorizacionesDetalle detalle)
    {
        foreach (var error in errores)
        {
            switch (error)
            {
                case ErroresFactura.NitEmisorInvalido:
                    detalle.CantidadRechazadasPorNitEmisorInvalido++;
                    break;
                case ErroresFactura.NitReceptorInvalido:
                    detalle.CantidadRechazadasPorNitReceptorInvalido++;
                    break;
                case ErroresFactura.NitEmisorInexistente:
                    detalle.CantidadRechazadasPorNitEmisorInexistente++;
                    break;
                case ErroresFactura.NitReceptorInexistente:
                    detalle.CantidadRechazadasPorNitReceptorInexistente++;
                    break;
                case ErroresFactura.IvaIncorrecto:
                    detalle.CantidadRechazadasPorIvaIncorrecto++;
                    break;
                case ErroresFactura.TotalIncorrecto:
                    detalle.CantidadRechazadasPorTotalIncorrecto++;
                    break;
                case ErroresFactura.ReferenciaDuplicada:
                    detalle.CantidadRechazadasPorReferenciaDuplicada++;
                    break;
                case ErroresFactura.ReferenciaInvalida:
                    detalle.CantidadRechazadasPorReferenciaInvalida++;
                    break;
                case ErroresFactura.FechaInvalida:
                    detalle.CantidadRechazadasPorFechaInvalida++;
                    break;
                case ErroresFactura.ValorInvalido:
                    detalle.CantidadRechazadasPorValorInvalido++;
                    break;
            }
        }
    }
}
