using System.ComponentModel.DataAnnotations;
using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos que el cliente puede enviar al crear una consulta. Estado, FechaCreacion e Id
/// los define el servidor: no se exponen para evitar over-posting.
/// </summary>
public class CrearConsultaRequest
{
    [Required(ErrorMessage = "El afiliado es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El afiliado es requerido.")]
    public int? AfiliadoId { get; set; }

    [Required(ErrorMessage = "El profesional es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El profesional es requerido.")]
    public int? ProfesionalId { get; set; }

    [Required(ErrorMessage = "La fecha y hora son requeridas.")]
    public DateTime? FechaHora { get; set; }

    [Required(ErrorMessage = "La modalidad es requerida.")]
    [EnumDataType(typeof(ModalidadConsulta), ErrorMessage = "La modalidad no es válida.")]
    public ModalidadConsulta? Modalidad { get; set; }

    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}
