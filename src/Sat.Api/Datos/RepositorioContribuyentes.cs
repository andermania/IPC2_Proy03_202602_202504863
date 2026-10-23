using Sat.Api.Dominio;
using Sat.Api.Entidades;
using Sat.Api.TDA;

namespace Sat.Api.Datos;

/// <summary>
/// Repositorio del Registro Tributario Unificado (RTU).
/// Mantiene los contribuyentes en memoria indexados por NIT en una TablaHash (TDA propio)
/// y los persiste en un archivo JSON (rtu.json).
/// </summary>
public class RepositorioContribuyentes
{
    private readonly string _archivo;
    private readonly object _candado = new();
    private readonly TablaHash<string, Contribuyente> _tabla = new();
    private readonly ListaEnlazada<Contribuyente> _orden = new();

    public RepositorioContribuyentes(string archivo)
    {
        _archivo = archivo;
        Cargar();
    }

    /// <summary>Busca un contribuyente por NIT (normalizado).</summary>
    public Contribuyente? Obtener(string nit)
    {
        lock (_candado)
        {
            return _tabla.IntentarObtener(ValidadorNIT.Normalizar(nit), out var contribuyente)
                ? contribuyente
                : null;
        }
    }

    /// <summary>Indica si el NIT está registrado en el RTU.</summary>
    public bool Existe(string nit) => Obtener(nit) is not null;

    /// <summary>Agrega un nuevo contribuyente al RTU.</summary>
    public void Insertar(Contribuyente contribuyente)
    {
        lock (_candado)
        {
            var clave = ValidadorNIT.Normalizar(contribuyente.NIT);
            if (_tabla.Contiene(clave))
            {
                return;
            }

            contribuyente.NIT = clave;
            _tabla.Insertar(clave, contribuyente);
            _orden.Agregar(contribuyente);
        }
    }

    /// <summary>Obtiene todos los contribuyentes en el orden en que fueron registrados.</summary>
    public ListaEnlazada<Contribuyente> ObtenerTodos()
    {
        lock (_candado)
        {
            var copia = new ListaEnlazada<Contribuyente>();
            foreach (var contribuyente in _orden)
            {
                copia.Agregar(contribuyente);
            }

            return copia;
        }
    }

    /// <summary>Persiste el RTU en el archivo JSON.</summary>
    public void Guardar()
    {
        lock (_candado)
        {
            AlmacenJson.Escribir(_archivo, _orden.AListaNativa());
        }
    }

    /// <summary>Vacía el RTU y elimina el archivo asociado.</summary>
    public void Limpiar()
    {
        lock (_candado)
        {
            _tabla.Limpiar();
            _orden.Limpiar();
            AlmacenJson.Eliminar(_archivo);
        }
    }

    private void Cargar()
    {
        lock (_candado)
        {
            var contribuyentes = AlmacenJson.Leer<List<Contribuyente>>(_archivo);
            if (contribuyentes is null)
            {
                return;
            }

            foreach (var contribuyente in contribuyentes)
            {
                if (string.IsNullOrWhiteSpace(contribuyente.NIT))
                {
                    continue;
                }

                var clave = ValidadorNIT.Normalizar(contribuyente.NIT);
                if (_tabla.Contiene(clave))
                {
                    continue;
                }

                contribuyente.NIT = clave;
                _tabla.Insertar(clave, contribuyente);
                _orden.Agregar(contribuyente);
            }
        }
    }
}
