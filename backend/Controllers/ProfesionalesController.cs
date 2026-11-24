using Microsoft.AspNetCore.Mvc;
using ConsultasPlan.Dtos;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ProfesionalesController : ControllerBase
{
    private readonly ICatalogoService _catalogo;

    public ProfesionalesController(ICatalogoService catalogo)
    {
        _catalogo = catalogo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProfesionalResponse>>> GetAll() =>
        Ok((await _catalogo.ObtenerProfesionalesAsync()).Select(p => p.ToResponse()));
}
