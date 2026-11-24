using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetAll() => Ok(await _afiliados.ObtenerTodosAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var afiliado = await _afiliados.ObtenerPorIdAsync(id);
        if (afiliado == null) return NotFound();
        return Ok(afiliado);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Afiliado afiliado)
    {
        var resultado = await _afiliados.CrearAsync(afiliado);
        if (!resultado.EsOk) return this.ToActionResult(resultado);
        return CreatedAtAction(nameof(GetById), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Afiliado afiliado) =>
        this.ToActionResult(await _afiliados.ActualizarAsync(id, afiliado));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _afiliados.EliminarAsync(id);
        if (!resultado.EsOk) return this.ToActionResult(resultado);
        return NoContent();
    }
}
