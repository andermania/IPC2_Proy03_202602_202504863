using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sat.Api.Entidades;
using Sat.Api.Infraestructura;
using Sat.Api.Servicios;

namespace Sat.Api.Controllers;

/// <summary>
/// Servicio de actualización y consulta del Registro Tributario Unificado (RTU).
/// </summary>
[ApiController]
[Route("api/rtu")]
public class RtuController : ControllerBase
{
    private readonly ServicioRTU _servicio;

    public RtuController(ServicioRTU servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Actualiza el RTU con el arreglo de contribuyentes recibido.</summary>
    [HttpPost]
    public async Task<ActionResult<ResultadoRTU>> Actualizar()
    {
        var cuerpo = await Mensajes.LeerCuerpoAsync(Request);

        if (cuerpo is null)
        {
            return BadRequest(new
            {
                error = "El mensaje no contiene JSON válido: el archivo está vacío, incompleto o con la sintaxis rota.",
            });
        }

        if (!Mensajes.Deserializar<Contribuyente>(cuerpo.Value, Mensajes.PropiedadesRTU, out var contribuyentes))
        {
            return BadRequest(new
            {
                error = "El mensaje no tiene la estructura JSON esperada para la actualización del RTU.",
            });
        }

        var resultado = _servicio.Actualizar(contribuyentes);
        return Ok(resultado);
    }

    /// <summary>Devuelve los contribuyentes registrados en el RTU.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<Contribuyente>> Consultar()
    {
        return Ok(_servicio.ObtenerTodos().AListaNativa());
    }
}
