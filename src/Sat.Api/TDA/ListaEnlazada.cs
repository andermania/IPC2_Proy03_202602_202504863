using System.Collections;

namespace Sat.Api.TDA;

/// <summary>
/// TDA ListaEnlazada: lista simplemente enlazada implementada por el estudiante.
/// Permite almacenar la información de los mensajes de entrada e interactuar con ella.
/// Implementa ICollection&lt;T&gt; para poder recorrerse con foreach y para que
/// System.Text.Json pueda serializarla y deserializarla en los archivos de persistencia.
/// </summary>
public class ListaEnlazada<T> : ICollection<T>
{
    private sealed class Nodo
    {
        public T Valor = default!;
        public Nodo? Siguiente;

        public Nodo(T valor) => Valor = valor;
    }

    private Nodo? _cabecera;
    private Nodo? _cola;
    private int _contador;

    /// <summary>Cantidad de elementos almacenados en la lista.</summary>
    public int Count => _contador;

    /// <summary>La lista siempre admite escritura.</summary>
    public bool IsReadOnly => false;

    /// <summary>Indica si la lista no contiene elementos.</summary>
    public bool EstaVacia => _cabecera is null;

    /// <summary>Agrega un elemento al final de la lista.</summary>
    public void Agregar(T valor)
    {
        var nodo = new Nodo(valor);

        if (_cabecera is null)
        {
            _cabecera = nodo;
            _cola = nodo;
        }
        else
        {
            _cola!.Siguiente = nodo;
            _cola = nodo;
        }

        _contador++;
    }

    /// <summary>Agrega un elemento al final de la lista (contrato ICollection).</summary>
    public void Add(T valor) => Agregar(valor);

    /// <summary>Elimina todos los elementos de la lista.</summary>
    public void Limpiar()
    {
        _cabecera = null;
        _cola = null;
        _contador = 0;
    }

    /// <summary>Elimina todos los elementos de la lista (contrato ICollection).</summary>
    public void Clear() => Limpiar();

    /// <summary>Busca el primer elemento que cumpla el criterio indicado.</summary>
    public bool IntentarBuscar(Func<T, bool> criterio, out T resultado)
    {
        for (var nodo = _cabecera; nodo is not null; nodo = nodo.Siguiente)
        {
            if (criterio(nodo.Valor))
            {
                resultado = nodo.Valor;
                return true;
            }
        }

        resultado = default!;
        return false;
    }

    /// <summary>Indica si algún elemento de la lista cumple el criterio indicado.</summary>
    public bool Contiene(Func<T, bool> criterio) => IntentarBuscar(criterio, out _);

    /// <summary>Indica si la lista contiene el elemento indicado (contrato ICollection).</summary>
    public bool Contains(T elemento) =>
        IntentarBuscar(valor => EqualityComparer<T>.Default.Equals(valor, elemento), out _);

    /// <summary>Elimina el primer elemento igual al indicado (contrato ICollection).</summary>
    public bool Remove(T elemento)
    {
        Nodo? anterior = null;

        for (var nodo = _cabecera; nodo is not null; nodo = nodo.Siguiente)
        {
            if (!EqualityComparer<T>.Default.Equals(nodo.Valor, elemento))
            {
                anterior = nodo;
                continue;
            }

            Desvincular(anterior, nodo);
            return true;
        }

        return false;
    }

    /// <summary>Copia los elementos a un arreglo nativo (contrato ICollection).</summary>
    public void CopyTo(T[] arreglo, int indiceArreglo)
    {
        if (arreglo is null)
        {
            throw new ArgumentNullException(nameof(arreglo));
        }

        if (indiceArreglo < 0 || indiceArreglo > arreglo.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(indiceArreglo));
        }

        if (arreglo.Length - indiceArreglo < _contador)
        {
            throw new ArgumentException("El arreglo de destino no tiene capacidad suficiente.");
        }

        foreach (var elemento in this)
        {
            arreglo[indiceArreglo++] = elemento;
        }
    }

    /// <summary>Devuelve una copia de los elementos en una List nativa de C#.</summary>
    public List<T> AListaNativa()
    {
        var lista = new List<T>(_contador);
        foreach (var elemento in this)
        {
            lista.Add(elemento);
        }

        return lista;
    }

    private void Desvincular(Nodo? anterior, Nodo nodo)
    {
        if (anterior is null)
        {
            _cabecera = nodo.Siguiente;
        }
        else
        {
            anterior.Siguiente = nodo.Siguiente;
        }

        if (ReferenceEquals(_cola, nodo))
        {
            _cola = anterior;
        }

        _contador--;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var nodo = _cabecera; nodo is not null; nodo = nodo.Siguiente)
        {
            yield return nodo.Valor;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
