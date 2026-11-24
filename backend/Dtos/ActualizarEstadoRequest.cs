using System.ComponentModel.DataAnnotations;
using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

public class ActualizarEstadoRequest
{
    // Nullable + Required: un body vacío ({}) tomaba el valor por defecto del enum (Pendiente)
    // y se interpretaba como un pedido de pasar la consulta a Pendiente.
    [Required(ErrorMessage = "El estado es requerido.")]
    [EnumDataType(typeof(EstadoConsulta), ErrorMessage = "El estado no es válido.")]
    public EstadoConsulta? Estado { get; set; }
}
