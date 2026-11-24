using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Helpers;
using ConsultasPlan.Models;

namespace ConsultasPlan.Services;

public interface IConsultaService
{
    Task<List<Consulta>> ObtenerTodasAsync();
    Task<Consulta?> ObtenerPorIdAsync(int id);
    Task<Resultado<Consulta>> CrearAsync(Consulta consulta);
    Task<Resultado<Consulta>> CancelarAsync(int id);
    Task<Resultado<Consulta>> MarcarInasistenciaAsync(int id);
    Task<Resultado<Consulta>> RegistrarAtencionAsync(int id);
    Task<Resultado<Consulta>> ActualizarEstadoAsync(int id, EstadoConsulta estado);
}

public class ConsultaService : IConsultaService
{
    private readonly AppDbContext _context;

    public ConsultaService(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Consulta>> ObtenerTodasAsync() =>
        _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .ToListAsync();

    public Task<Consulta?> ObtenerPorIdAsync(int id) =>
        _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Resultado<Consulta>> CrearAsync(Consulta consulta)
    {
        var afiliado = await _context.Afiliados.FindAsync(consulta.AfiliadoId);
        if (afiliado == null)
            return Resultado<Consulta>.NoEncontrado("Afiliado no encontrado.");

        if (afiliado.Bloqueado)
            return Resultado<Consulta>.Invalido("El afiliado se encuentra bloqueado para agendar consultas.");

        var profesionalExiste = await _context.Profesionales.AnyAsync(p => p.Id == consulta.ProfesionalId);
        if (!profesionalExiste)
            return Resultado<Consulta>.NoEncontrado("Profesional no encontrado.");

        var consultaConflicto = await _context.Consultas.AnyAsync(c =>
            c.ProfesionalId == consulta.ProfesionalId &&
            c.FechaHora == consulta.FechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        if (consultaConflicto)
            return Resultado<Consulta>.Invalido("El profesional ya tiene una consulta en ese horario.");

        consulta.FechaCreacion = DateTime.UtcNow;
        consulta.Estado = EstadoConsulta.Pendiente;
        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(consulta);
    }

    public async Task<Resultado<Consulta>> CancelarAsync(int id)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return Resultado<Consulta>.NoEncontrado();

        if (consulta.FechaHora - DateTime.Now < TimeSpan.FromHours(24))
            return Resultado<Consulta>.Invalido("No se puede cancelar con menos de 24 horas de anticipación.");

        consulta.Estado = EstadoConsulta.Cancelado;
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(consulta);
    }

    public async Task<Resultado<Consulta>> MarcarInasistenciaAsync(int id)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return Resultado<Consulta>.NoEncontrado();

        if (consulta.FechaHora > DateTime.Now)
            return Resultado<Consulta>.Invalido("No se puede registrar una inasistencia sobre una consulta futura.");

        consulta.Estado = EstadoConsulta.Inasistencia;
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(consulta);
    }

    public async Task<Resultado<Consulta>> RegistrarAtencionAsync(int id)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return Resultado<Consulta>.NoEncontrado();

        if (!consulta.FechaHora.IsInCurrentYear())
            return Resultado<Consulta>.Invalido("Solo se pueden registrar atenciones de consultas del año en curso.");

        consulta.Estado = EstadoConsulta.Atendido;
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(consulta);
    }

    public async Task<Resultado<Consulta>> ActualizarEstadoAsync(int id, EstadoConsulta estado)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return Resultado<Consulta>.NoEncontrado();

        consulta.Estado = estado;
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(consulta);
    }
}
