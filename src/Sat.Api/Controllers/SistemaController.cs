using Microsoft.AspNetCore.Mvc;
using Sat.Api.Datos;
using Sat.Api.Dominio;

namespace Sat.Api.Controllers;

/// <summary>
/// Operaciones generales del sistema: inicialización (estado sin datos) y estado del servicio.
/// </summary>
[ApiController]
[Route("api/sistema")]
public class SistemaController : ControllerBase
{
    private readonly RepositorioContribuyentes _contribuyentes;
    private readonly RepositorioFacturas _facturas;
    private readonly GeneradorCorrelativo _correlativo;

    public SistemaController(
        RepositorioContribuyentes contribuyentes,
        RepositorioFacturas facturas,
        GeneradorCorrelativo correlativo)
    {
        _contribuyentes = contribuyentes;
        _facturas = facturas;
        _correlativo = correlativo;
    }

    /// <summary>Devuelve el sistema al estado inicial, es decir, sin datos.</summary>
    [HttpPost("inicializar")]
    public IActionResult Inicializar()
    {
        _contribuyentes.Limpiar();
        _facturas.Limpiar();
        _correlativo.Limpiar();

        return Ok(new
        {
            estado = "inicializado",
            contribuyentes = 0,
            facturas = 0,
        });
    }

    /// <summary>Verifica que el servicio esté operativo.</summary>
    [HttpGet("salud")]
    public IActionResult Salud() => Ok(new { estado = "ok" });
}
