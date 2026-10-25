using Sat.Api.Datos;
using Sat.Api.Dominio;
using Sat.Api.Entidades;
using Sat.Api.TDA;

namespace Sat.Api.Servicios;

/// <summary>
/// Servicio encargado del Registro Tributario Unificado (RTU):
/// crea, actualiza y consulta contribuyentes. Un NIT inválido no tiene ningún efecto
/// sobre el RTU.
/// </summary>
public class ServicioRTU
{
    private readonly RepositorioContribuyentes _repositorio;

    public ServicioRTU(RepositorioContribuyentes repositorio)
    {
        _repositorio = repositorio;
    }

    /// <summary>
    /// Procesa el mensaje de actualización del RTU y devuelve el resumen de la operación.
    /// </summary>
    public ResultadoRTU Actualizar(IEnumerable<Contribuyente> contribuyentes)
    {
        var resultado = new ResultadoRTU();

        foreach (var contribuyente in contribuyentes)
        {
            if (!ValidadorNIT.EsValido(contribuyente.NIT))
            {
                // NIT inválido: no tiene ningún efecto sobre el RTU.
                resultado.NitsInvalidos++;
                continue;
            }

            var nit = ValidadorNIT.Normalizar(contribuyente.NIT);
            var existente = _repositorio.Obtener(nit);

            if (existente is null)
            {
                _repositorio.Insertar(new Contribuyente
                {
                    NIT = nit,
                    Nombre = contribuyente.Nombre ?? string.Empty,
                });
                resultado.ContribuyentesNuevos++;
            }
            else
            {
                // Solo se actualiza la razón social si el mensaje la incluye, de modo
                // que un mensaje sin "nombre" no borre el nombre ya registrado.
                if (!string.IsNullOrWhiteSpace(contribuyente.Nombre))
                {
                    existente.Nombre = contribuyente.Nombre.Trim();
                }

                resultado.ContribuyentesActualizados++;
            }
        }

        _repositorio.Guardar();
        return resultado;
    }

    /// <summary>Verifica que el contribuyente exista en el RTU con un NIT válido.</summary>
    public bool Existe(string? nit) => _repositorio.Obtener(ValidadorNIT.Normalizar(nit)) is not null;

    /// <summary>Devuelve todos los contribuyentes registrados.</summary>
    public ListaEnlazada<Contribuyente> ObtenerTodos() => _repositorio.ObtenerTodos();
}
