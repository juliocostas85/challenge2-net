using Microsoft.AspNetCore.Mvc;
using ConsultasPlan.Dtos;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly IConsultaService _consultas;

    public ConsultasController(IConsultaService consultas)
    {
        _consultas = consultas;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConsultaResponse>>> GetAll() =>
        Ok((await _consultas.ObtenerTodasAsync()).Select(c => c.ToResponse()));

    [HttpGet("{id}")]
    public async Task<ActionResult<ConsultaResponse>> GetById(int id)
    {
        var consulta = await _consultas.ObtenerPorIdAsync(id);
        if (consulta == null) return NotFound();
        return Ok(consulta.ToResponse());
    }

    [HttpPost]
    public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaRequest request)
    {
        var resultado = await _consultas.CrearAsync(request);
        if (!resultado.EsOk) return this.ToActionResult(resultado, c => c.ToResponse());
        return CreatedAtAction(nameof(GetById), new { id = resultado.Valor!.Id }, resultado.Valor.ToResponse());
    }

    // Las acciones sobre una consulta comparten la forma POST /consultas/{id}/<acción>.
    // Antes la cancelación era la única con la acción antes del id (PUT /consultas/cancelar/{id}).
    [HttpPost("{id}/cancelacion")]
    public async Task<IActionResult> CancelarConsulta(int id) =>
        this.ToActionResult(await _consultas.CancelarAsync(id), c => c.ToResponse());

    [HttpPost("{id}/inasistencia")]
    public async Task<IActionResult> MarcarInasistencia(int id) =>
        this.ToActionResult(await _consultas.MarcarInasistenciaAsync(id), c => c.ToResponse());

    [HttpPost("{id}/atencion")]
    public async Task<IActionResult> RegistrarAtencion(int id) =>
        this.ToActionResult(await _consultas.RegistrarAtencionAsync(id), c => c.ToResponse());

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request) =>
        this.ToActionResult(await _consultas.ActualizarEstadoAsync(id, request.Estado!.Value), c => c.ToResponse());
}
