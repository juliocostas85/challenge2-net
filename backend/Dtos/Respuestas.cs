using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

// Contratos de salida de la API. Desacoplan la respuesta del esquema de la base: las entidades EF
// exponían todos sus campos (incluidos los internos) y obligaban a ignorar referencias circulares.
// Los nombres de los campos se mantienen para no romper a los clientes existentes.

public record SedeResponse(int Id, string Nombre, string Direccion);

public record ProfesionalResponse(int Id, string NombreCompleto, string Especialidad, int SedeId, SedeResponse? Sede);

/// <summary>
/// Cupo de nutrición del año en curso, calculado a partir del historial de consultas
/// (reemplaza al contador ConsultasUsadas, que ninguna operación actualizaba).
/// </summary>
public record CupoNutricionResponse(int Anio, int Maximo, int Usadas, int Disponibles, bool UsadaEsteMes);

public record AfiliadoResponse(
    int Id,
    string NombreCompleto,
    string DNI,
    string Email,
    string Telefono,
    string Plan,
    CupoNutricionResponse? CupoNutricion,
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

    public static CupoNutricionResponse ToResponse(this EstadoCupo c) =>
        new(c.Anio, c.MaximoAnual, c.Usadas, c.Disponibles, c.UsadaEnMes);

    public static AfiliadoResponse ToResponse(this Afiliado a, EstadoCupo? cupo) =>
        new(a.Id, a.NombreCompleto, a.DNI, a.Email, a.Telefono, a.Plan, cupo?.ToResponse(), a.Bloqueado, a.FechaBloqueo, a.IsActive);

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
