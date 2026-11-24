using Microsoft.AspNetCore.Mvc;
using ConsultasPlan.Dtos;
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
    public async Task<ActionResult<IEnumerable<AfiliadoResponse>>> GetAll() =>
        Ok((await _afiliados.ObtenerTodosAsync()).Select(a => a.ToResponse()));

    [HttpGet("{id}")]
    public async Task<ActionResult<AfiliadoResponse>> GetById(int id)
    {
        var afiliado = await _afiliados.ObtenerPorIdAsync(id);
        if (afiliado == null) return NotFound();
        return Ok(afiliado.ToResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AfiliadoRequest request)
    {
        var resultado = await _afiliados.CrearAsync(request);
        if (!resultado.EsOk) return this.ToActionResult(resultado, a => a.ToResponse());
        return CreatedAtAction(nameof(GetById), new { id = resultado.Valor!.Id }, resultado.Valor.ToResponse());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AfiliadoRequest request) =>
        this.ToActionResult(await _afiliados.ActualizarAsync(id, request), a => a.ToResponse());

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _afiliados.EliminarAsync(id);
        if (!resultado.EsOk) return this.ToActionResult(resultado, a => a.ToResponse());
        return NoContent();
    }
}
