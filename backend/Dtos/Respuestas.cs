using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

// Contratos de salida de la API. Desacoplan la respuesta del esquema de la base: las entidades EF
// exponían todos sus campos (incluidos los internos) y obligaban a ignorar referencias circulares.
// Los nombres de los campos se mantienen para no romper a los clientes existentes.

public record SedeResponse(int Id, string Nombre, string Direccion);

public record ProfesionalResponse(int Id, string NombreCompleto, string Especialidad, int SedeId, SedeResponse? Sede);

public record AfiliadoResponse(
    int Id,
    string NombreCompleto,
    string DNI,
    string Email,
    string Telefono,
    string Plan,
    int ConsultasUsadas,
    bool Bloqueado,
    DateTime? FechaBloqueo,
    bool Activo);

/// <summary>Datos del afiliado que se muestran junto a una consulta.</summary>
public record AfiliadoResumenResponse(int Id, string NombreCompleto, string DNI, bool Activo);

public record ConsultaResponse(
    int Id,
    int AfiliadoId,
    AfiliadoResumenResponse? Afiliado,
    int ProfesionalId,
    ProfesionalResponse? Profesional,
    DateTime FechaHora,
    EstadoConsulta Estado,
    ModalidadConsulta Modalidad,
    string Motivo,
    DateTime FechaCreacion);

public static class RespuestasMapper
{
    public static SedeResponse ToResponse(this Sede s) => new(s.Id, s.Nombre, s.Direccion);

    public static ProfesionalResponse ToResponse(this Profesional p) =>
        new(p.Id, p.NombreCompleto, p.Especialidad, p.SedeId, p.Sede?.ToResponse());

    public static AfiliadoResponse ToResponse(this Afiliado a) =>
        new(a.Id, a.NombreCompleto, a.DNI, a.Email, a.Telefono, a.Plan, a.ConsultasUsadas, a.Bloqueado, a.FechaBloqueo, a.IsActive);

    public static ConsultaResponse ToResponse(this Consulta c) =>
        new(c.Id,
            c.AfiliadoId,
            c.Afiliado is null ? null : new AfiliadoResumenResponse(c.Afiliado.Id, c.Afiliado.NombreCompleto, c.Afiliado.DNI, c.Afiliado.IsActive),
            c.ProfesionalId,
            c.Profesional?.ToResponse(),
            c.FechaHora,
            c.Estado,
            c.Modalidad,
            c.Motivo,
            c.FechaCreacion);
}
