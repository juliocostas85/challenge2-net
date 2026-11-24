using System.ComponentModel.DataAnnotations;

namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos editables de un afiliado. Bloqueado, ConsultasUsadas, IsActive y CreatedAt
/// los gestiona el sistema: no se aceptan desde el cliente para evitar over-posting.
/// </summary>
public class AfiliadoRequest
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El DNI es requerido.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos, sin puntos.")]
    public string DNI { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string Telefono { get; set; } = string.Empty;
}
