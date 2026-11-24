using Microsoft.AspNetCore.Mvc;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
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
    public async Task<IActionResult> GetAll() => Ok(await _consultas.ObtenerTodasAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await _consultas.ObtenerPorIdAsync(id);
        if (consulta == null) return NotFound();
        return Ok(consulta);
    }

    [HttpPost]
    public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaRequest request)
    {
        var resultado = await _consultas.CrearAsync(request);
        if (!resultado.EsOk) return this.ToActionResult(resultado);
        return CreatedAtAction(nameof(GetById), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpPut("cancelar/{id}")]
    public async Task<IActionResult> CancelarConsulta(int id) =>
        this.ToActionResult(await _consultas.CancelarAsync(id));

    [HttpPost("{id}/inasistencia")]
    public async Task<IActionResult> MarcarInasistencia(int id) =>
        this.ToActionResult(await _consultas.MarcarInasistenciaAsync(id));

    [HttpPost("{id}/atencion")]
    public async Task<IActionResult> RegistrarAtencion(int id) =>
        this.ToActionResult(await _consultas.RegistrarAtencionAsync(id));

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request) =>
        this.ToActionResult(await _consultas.ActualizarEstadoAsync(id, request.Estado));
}

public class ActualizarEstadoRequest
{
    public EstadoConsulta Estado { get; set; }
}
