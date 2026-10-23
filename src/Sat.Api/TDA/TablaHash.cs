namespace Sat.Api.TDA;

/// <summary>
/// TDA TablaHash: tabla de dispersión (hash) con encadenamiento separado, implementada
/// por el estudiante. Se utiliza para mantener el RTU indexado por NIT, para verificar
/// rapidamente referencias duplicadas y como índice auxiliar de consultas.
/// La función de dispersión propia (djb2) se aplica a las claves de tipo string.
/// </summary>
public class TablaHash<K, V> where K : notnull
{
    private sealed class Par
    {
        public K Clave = default!;
        public V Valor = default!;
    }

    private ListaEnlazada<Par>[] _cubetas;
    private int _cantidad;
    private readonly IEqualityComparer<K> _comparador;

    public TablaHash(int capacidadInicial = 31, IEqualityComparer<K>? comparador = null)
    {
        if (capacidadInicial < 1)
        {
            capacidadInicial = 1;
        }

        _cubetas = CrearCubetas(capacidadInicial);
        _comparador = comparador ?? EqualityComparer<K>.Default;
    }

    /// <summary>Cantidad de claves almacenadas en la tabla.</summary>
    public int Count => _cantidad;

    /// <summary>
    /// Inserta una clave o actualiza el valor si la clave ya existía.
    /// Devuelve true si la clave era nueva.
    /// </summary>
    public bool Insertar(K clave, V valor)
    {
        var cubeta = _cubetas[Indice(clave)];

        foreach (var elemento in cubeta)
        {
            if (_comparador.Equals(elemento.Clave, clave))
            {
                // La clave ya existía: se actualiza el valor.
                elemento.Valor = valor;
                return false;
            }
        }

        cubeta.Agregar(new Par { Clave = clave, Valor = valor });
        _cantidad++;

        if (_cantidad > _cubetas.Length * 0.75)
        {
            Redimensionar();
        }

        return true;
    }

    /// <summary>Obtiene el valor asociado a la clave indicada.</summary>
    public bool IntentarObtener(K clave, out V valor)
    {
        var cubeta = _cubetas[Indice(clave)];
        if (cubeta.IntentarBuscar(p => _comparador.Equals(p.Clave, clave), out var par))
        {
            valor = par.Valor;
            return true;
        }

        valor = default!;
        return false;
    }

    /// <summary>Obtiene el valor asociado a la clave o el valor por defecto.</summary>
    public V? Obtener(K clave) => IntentarObtener(clave, out var valor) ? valor : default;

    /// <summary>Indica si la clave existe en la tabla.</summary>
    public bool Contiene(K clave)
    {
        var cubeta = _cubetas[Indice(clave)];
        return cubeta.Contiene(p => _comparador.Equals(p.Clave, clave));
    }

    /// <summary>Elimina la clave indicada. Devuelve true si existía.</summary>
    public bool Eliminar(K clave)
    {
        var indice = Indice(clave);
        var cubeta = _cubetas[indice];
        if (!cubeta.Contiene(p => _comparador.Equals(p.Clave, clave)))
        {
            return false;
        }

        var nueva = new ListaEnlazada<Par>();
        foreach (var par in cubeta)
        {
            if (!_comparador.Equals(par.Clave, clave))
            {
                nueva.Agregar(par);
            }
        }

        _cubetas[indice] = nueva;
        _cantidad--;
        return true;
    }

    /// <summary>Elimina todas las claves de la tabla.</summary>
    public void Limpiar()
    {
        _cubetas = CrearCubetas(31);
        _cantidad = 0;
    }

    /// <summary>Recorre todos los pares (clave, valor) almacenados.</summary>
    public IEnumerable<(K Clave, V Valor)> Elementos()
    {
        foreach (var cubeta in _cubetas)
        {
            foreach (var par in cubeta)
            {
                yield return (par.Clave, par.Valor);
            }
        }
    }

    private static ListaEnlazada<Par>[] CrearCubetas(int cantidad)
    {
        var cubetas = new ListaEnlazada<Par>[cantidad];
        for (var i = 0; i < cubetas.Length; i++)
        {
            cubetas[i] = new ListaEnlazada<Par>();
        }

        return cubetas;
    }

    private void Redimensionar()
    {
        var anteriores = _cubetas;
        _cubetas = CrearCubetas(anteriores.Length * 2 + 1);
        _cantidad = 0;

        foreach (var cubeta in anteriores)
        {
            foreach (var par in cubeta)
            {
                InsertarDirecto(par.Clave, par.Valor);
            }
        }
    }

    private void InsertarDirecto(K clave, V valor)
    {
        var cubeta = _cubetas[Indice(clave)];
        cubeta.Agregar(new Par { Clave = clave, Valor = valor });
        _cantidad++;
    }

    private int Indice(K clave)
    {
        ulong hash;

        if (clave is string texto)
        {
            // Dispersión propia estilo djb2 (algoritmo implementado por el estudiante).
            hash = 5381;
            foreach (var caracter in texto)
            {
                hash = hash * 33 + (uint)caracter;
            }
        }
        else
        {
            hash = (uint)_comparador.GetHashCode(clave);
        }

        return (int)(hash % (ulong)_cubetas.Length);
    }
}
