using Microsoft.AspNetCore.Mvc;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class AfiliadosController : ControllerBase
{
    private readonly IAfiliadoService _afiliados;

    public AfiliadosController(IAfiliadoService afiliados)
    {
        _afiliados = afiliados;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AfiliadoResponse>>> GetAll()
    {
        var afiliados = await _afiliados.ObtenerTodosAsync();
        var cupos = await _afiliados.ObtenerCupoNutricionAsync(afiliados.Select(a => a.Id).ToList());
        return Ok(afiliados.Select(a => a.ToResponse(cupos[a.Id])));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AfiliadoResponse>> GetById(int id)
    {
        var afiliado = await _afiliados.ObtenerPorIdAsync(id);
        if (afiliado == null) return NotFound();
        return Ok(await ConCupoAsync(afiliado));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AfiliadoRequest request)
    {
        var resultado = await _afiliados.CrearAsync(request);
        if (!resultado.EsOk) return this.ToActionResult(resultado, a => a.ToResponse(null));
        return CreatedAtAction(nameof(GetById), new { id = resultado.Valor!.Id }, await ConCupoAsync(resultado.Valor));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AfiliadoRequest request)
    {
        var resultado = await _afiliados.ActualizarAsync(id, request);
        if (!resultado.EsOk) return this.ToActionResult(resultado, a => a.ToResponse(null));
        return Ok(await ConCupoAsync(resultado.Valor!));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _afiliados.EliminarAsync(id);
        if (!resultado.EsOk) return this.ToActionResult(resultado, a => a.ToResponse(null));
        return NoContent();
    }

    private async Task<AfiliadoResponse> ConCupoAsync(Afiliado afiliado)
    {
        var cupos = await _afiliados.ObtenerCupoNutricionAsync([afiliado.Id]);
        return afiliado.ToResponse(cupos[afiliado.Id]);
    }
}
