using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sat.Api.Entidades;
using Sat.Api.Infraestructura;
using Sat.Api.Servicios;

namespace Sat.Api.Controllers;

/// <summary>
/// Servicio de autorización de Documentos Tributarios Electrónicos (DTE).
/// Recibe el mensaje JSON con las solicitudes y devuelve el número único de
/// autorización de cada factura aprobada.
/// </summary>
[ApiController]
[Route("api/autorizaciones")]
public class AutorizacionesController : ControllerBase
{
    private readonly ProcesadorAutorizaciones _procesador;

    public AutorizacionesController(ProcesadorAutorizaciones procesador)
    {
        _procesador = procesador;
    }

    /// <summary>Procesa un arreglo de solicitudes de autorización.</summary>
    [HttpPost]
    public ActionResult<ResultadoAutorizaciones> Autorizar([FromBody] JsonElement cuerpo)
    {
        if (!Mensajes.Deserializar<Factura>(cuerpo, out var solicitudes))
        {
            return BadRequest(new
            {
                error = "El mensaje no tiene la estructura JSON esperada para solicitudes de autorización.",
            });
        }

        var resultado = _procesador.Procesar(solicitudes);
        return Ok(resultado);
    }
}
