using Microsoft.AspNetCore.Mvc;
using ConsultasPlan.Services;

namespace ConsultasPlan.Controllers;

public static class ResultadoExtensions
{
    /// <summary>
    /// Traduce un resultado de negocio a la respuesta HTTP, con el formato { mensaje } que consume el frontend.
    /// <paramref name="mapear"/> convierte la entidad al contrato de salida.
    /// </summary>
    public static IActionResult ToActionResult<T, TResponse>(this ControllerBase controller, Resultado<T> resultado, Func<T, TResponse> mapear) =>
        resultado.Tipo switch
        {
            TipoResultado.Ok           => controller.Ok(mapear(resultado.Valor!)),
            TipoResultado.NoEncontrado => resultado.Mensaje is null
                                            ? controller.NotFound()
                                            : controller.NotFound(new { mensaje = resultado.Mensaje }),
            TipoResultado.Conflicto    => controller.Conflict(new { mensaje = resultado.Mensaje }),
            _                          => controller.BadRequest(new { mensaje = resultado.Mensaje })
        };
}
