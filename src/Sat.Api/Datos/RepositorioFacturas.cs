using Sat.Api.Entidades;
using Sat.Api.TDA;

namespace Sat.Api.Datos;

/// <summary>
/// Repositorio de las facturas recibidas por el servicio (autorizadas y rechazadas).
/// Mantiene los registros en una ListaEnlazada (TDA propio) y un índice en TablaHash
/// con las referencias ya utilizadas, para detectar duplicados de forma eficiente.
/// La información se persiste en un archivo JSON (facturas.json).
/// </summary>
public class RepositorioFacturas
{
    private readonly string _archivo;
    private readonly object _candado = new();
    private readonly ListaEnlazada<RegistroFactura> _registros = new();
    private readonly TablaHash<string, bool> _referencias = new();

    public RepositorioFacturas(string archivo)
    {
        _archivo = archivo;
        Cargar();
    }

    /// <summary>Indica si la referencia ya fue utilizada por una factura anterior.</summary>
    public bool ExisteReferencia(string referencia)
    {
        lock (_candado)
        {
            return _referencias.Contiene(referencia);
        }
    }

    /// <summary>Reserva una referencia para que no pueda volver a utilizarse.</summary>
    public void ReservarReferencia(string referencia)
    {
        lock (_candado)
        {
            _referencias.Insertar(referencia, true);
        }
    }

    /// <summary>Agrega los registros procesados y los persiste en disco.</summary>
    public void Agregar(ListaEnlazada<RegistroFactura> registros)
    {
        lock (_candado)
        {
            foreach (var registro in registros)
            {
                _registros.Agregar(registro);

                if (registro.Referencia.Length is >= 1 and <= 40)
                {
                    _referencias.Insertar(registro.Referencia, true);
                }
            }

            GuardarInterno();
        }
    }

    /// <summary>Obtiene todas las facturas registradas en orden de llegada.</summary>
    public ListaEnlazada<RegistroFactura> ObtenerTodos()
    {
        lock (_candado)
        {
            var copia = new ListaEnlazada<RegistroFactura>();
            foreach (var registro in _registros)
            {
                copia.Agregar(registro);
            }

            return copia;
        }
    }

    /// <summary>Cantidad total de facturas recibidas.</summary>
    public int Cantidad
    {
        get
        {
            lock (_candado)
            {
                return _registros.Count;
            }
        }
    }

    /// <summary>Vacía el repositorio y elimina el archivo asociado.</summary>
    public void Limpiar()
    {
        lock (_candado)
        {
            _registros.Limpiar();
            _referencias.Limpiar();
            AlmacenJson.Eliminar(_archivo);
        }
    }

    private void GuardarInterno() => AlmacenJson.Escribir(_archivo, _registros.AListaNativa());

    private void Cargar()
    {
        lock (_candado)
        {
            var registros = AlmacenJson.Leer<List<RegistroFactura>>(_archivo);
            if (registros is null)
            {
                return;
            }

            foreach (var registro in registros)
            {
                registro.Errores ??= new ListaEnlazada<string>();
                _registros.Agregar(registro);

                if (registro.Referencia.Length is >= 1 and <= 40)
                {
                    _referencias.Insertar(registro.Referencia, true);
                }
            }
        }
    }
}
