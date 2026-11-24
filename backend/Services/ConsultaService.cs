using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Services;

public interface IConsultaService
{
    Task<List<Consulta>> ObtenerTodasAsync();
    Task<Consulta?> ObtenerPorIdAsync(int id);
    Task<Resultado<Consulta>> CrearAsync(CrearConsultaRequest request);
    Task<Resultado<Consulta>> CancelarAsync(int id);
    Task<Resultado<Consulta>> MarcarInasistenciaAsync(int id);
    Task<Resultado<Consulta>> RegistrarAtencionAsync(int id);
    Task<Resultado<Consulta>> ActualizarEstadoAsync(int id, EstadoConsulta estado);
}

public class ConsultaService : IConsultaService
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _tiempo;

    public ConsultaService(AppDbContext context, TimeProvider tiempo)
    {
        _context = context;
        _tiempo = tiempo;
    }

    /// <summary>
    /// Hora local de la clínica. Las fechas de consulta llegan desde el frontend como hora local
    /// (input datetime-local, sin zona), por lo que todas las comparaciones se hacen en hora local.
    /// </summary>
    private DateTime Ahora => _tiempo.GetLocalNow().DateTime;

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

    public async Task<Resultado<Consulta>> CrearAsync(CrearConsultaRequest request)
    {
        var consulta = new Consulta
        {
            AfiliadoId = request.AfiliadoId!.Value,
            ProfesionalId = request.ProfesionalId!.Value,
            FechaHora = request.FechaHora!.Value,
            Modalidad = request.Modalidad!.Value,
            Motivo = request.Motivo.Trim()
        };

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

        consulta.FechaCreacion = Ahora;
        consulta.Estado = EstadoConsulta.Pendiente;
        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(consulta);
    }

    public Task<Resultado<Consulta>> CancelarAsync(int id) =>
        CambiarEstadoAsync(id, EstadoConsulta.Cancelado);

    public Task<Resultado<Consulta>> MarcarInasistenciaAsync(int id) =>
        CambiarEstadoAsync(id, EstadoConsulta.Inasistencia);

    public Task<Resultado<Consulta>> RegistrarAtencionAsync(int id) =>
        CambiarEstadoAsync(id, EstadoConsulta.Atendido);

    public Task<Resultado<Consulta>> ActualizarEstadoAsync(int id, EstadoConsulta estado) =>
        CambiarEstadoAsync(id, estado);

    /// <summary>
    /// Único punto donde cambia el estado de una consulta: todas las operaciones aplican
    /// las mismas reglas de ciclo de vida (ver ConsultaTransiciones).
    /// </summary>
    private async Task<Resultado<Consulta>> CambiarEstadoAsync(int id, EstadoConsulta destino)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return Resultado<Consulta>.NoEncontrado();

        var error = ConsultaTransiciones.Validar(consulta, destino, Ahora);
        if (error != null)
            return error.EsConflictoDeEstado
                ? Resultado<Consulta>.Conflicto(error.Mensaje)
                : Resultado<Consulta>.Invalido(error.Mensaje);

        consulta.Estado = destino;
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(consulta);
    }
}
