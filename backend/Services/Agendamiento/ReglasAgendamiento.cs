using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Models;

namespace ConsultasPlan.Services.Agendamiento;

/// <summary>Datos de una consulta que se quiere agendar, ya resueltos (afiliado y profesional existen).</summary>
public record SolicitudAgendamiento(Consulta Consulta, Afiliado Afiliado, Profesional Profesional);

/// <summary>
/// Regla de negocio que debe cumplirse para agendar una consulta.
/// ConsultaService aplica todas las reglas registradas en el contenedor, en el orden de registro:
/// agregar una regla nueva es agregar una clase y registrarla, sin modificar el servicio (OCP).
/// </summary>
public interface IReglaAgendamiento
{
    /// <summary>Null si la regla se cumple; si no, el motivo para informar al usuario.</summary>
    Task<string?> ValidarAsync(SolicitudAgendamiento solicitud);
}

/// <summary>Solo afiliados activos y no bloqueados pueden agendar.</summary>
public class AfiliadoHabilitado : IReglaAgendamiento
{
    public Task<string?> ValidarAsync(SolicitudAgendamiento solicitud) =>
        Task.FromResult(
            !solicitud.Afiliado.IsActive ? "El afiliado está dado de baja."
            : solicitud.Afiliado.Bloqueado ? "El afiliado se encuentra bloqueado para agendar consultas."
            : null);
}

/// <summary>El profesional no puede tener otra consulta no cancelada en el mismo horario.</summary>
public class HorarioProfesionalDisponible : IReglaAgendamiento
{
    private readonly AppDbContext _context;

    public HorarioProfesionalDisponible(AppDbContext context) => _context = context;

    public async Task<string?> ValidarAsync(SolicitudAgendamiento solicitud)
    {
        var ocupado = await _context.Consultas.AnyAsync(c =>
            c.ProfesionalId == solicitud.Consulta.ProfesionalId &&
            c.FechaHora == solicitud.Consulta.FechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        return ocupado ? "El profesional ya tiene una consulta en ese horario." : null;
    }
}

/// <summary>El afiliado no puede tener otra consulta no cancelada en el mismo horario.</summary>
public class HorarioAfiliadoDisponible : IReglaAgendamiento
{
    private readonly AppDbContext _context;

    public HorarioAfiliadoDisponible(AppDbContext context) => _context = context;

    public async Task<string?> ValidarAsync(SolicitudAgendamiento solicitud)
    {
        var ocupado = await _context.Consultas.AnyAsync(c =>
            c.AfiliadoId == solicitud.Consulta.AfiliadoId &&
            c.FechaHora == solicitud.Consulta.FechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        return ocupado ? "El afiliado ya tiene una consulta en ese horario." : null;
    }
}
