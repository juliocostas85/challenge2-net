using Microsoft.AspNetCore.Mvc;
using ConsultasPlan.Dtos;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class SedesController : ControllerBase
{
    private readonly ICatalogoService _catalogo;

    public SedesController(ICatalogoService catalogo)
    {
        _catalogo = catalogo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SedeResponse>>> GetAll() =>
        Ok((await _catalogo.ObtenerSedesAsync()).Select(s => s.ToResponse()));
}
