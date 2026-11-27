using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Domain;
using ConsultasPlan.Models;
using ConsultasPlan.Services.Agendamiento;

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
    private readonly IReadOnlyList<IReglaAgendamiento> _reglas;

    public ConsultaService(AppDbContext context, TimeProvider tiempo, IEnumerable<IReglaAgendamiento> reglas)
    {
        _context = context;
        _tiempo = tiempo;
        _reglas = reglas.ToList();
    }

    private DateTime Ahora => _tiempo.AhoraLocal();

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
            FechaHora = NormalizarFechaHora(request.FechaHora!.Value),
            Modalidad = request.Modalidad!.Value,
            Motivo = request.Motivo.Trim()
        };

        if (consulta.FechaHora <= Ahora)
            return Resultado<Consulta>.Invalido("La consulta debe agendarse en una fecha y hora futura.");

        var afiliado = await _context.Afiliados.FindAsync(consulta.AfiliadoId);
        if (afiliado == null)
            return Resultado<Consulta>.NoEncontrado("Afiliado no encontrado.");

        var profesional = await _context.Profesionales.FindAsync(consulta.ProfesionalId);
        if (profesional == null)
            return Resultado<Consulta>.NoEncontrado("Profesional no encontrado.");

        // Reglas de negocio del agendamiento (habilitación, horarios, cupos...). Cada una es una clase
        // registrada en el contenedor: agregar una regla no requiere modificar este servicio.
        var solicitud = new SolicitudAgendamiento(consulta, afiliado, profesional);
        foreach (var regla in _reglas)
        {
            var error = await regla.ValidarAsync(solicitud);
            if (error != null) return Resultado<Consulta>.Invalido(error);
        }

        consulta.FechaCreacion = Ahora;
        consulta.Estado = EstadoConsulta.Pendiente;
        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();
        return Resultado<Consulta>.Ok(await CargarCompletaAsync(consulta.Id));
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
        return Resultado<Consulta>.Ok(await CargarCompletaAsync(consulta.Id));
    }

    /// <summary>
    /// Convención del sistema: FechaHora es hora local de la clínica, con precisión de minuto.
    /// - Si llega con zona (ej. "...Z" desde Swagger) se convierte a hora local; si no, se guardaba
    ///   la hora UTC como si fuera local (3 h de diferencia en Argentina).
    /// - Se descartan segundos y milisegundos: el control de horario ocupado compara por igualdad
    ///   y 10:00:30 esquivaba una consulta existente a las 10:00.
    /// </summary>
    private DateTime NormalizarFechaHora(DateTime fecha)
    {
        if (fecha.Kind != DateTimeKind.Unspecified)
            fecha = TimeZoneInfo.ConvertTimeFromUtc(fecha.ToUniversalTime(), _tiempo.LocalTimeZone);
        return new DateTime(fecha.Year, fecha.Month, fecha.Day, fecha.Hour, fecha.Minute, 0, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Todas las operaciones devuelven la consulta con la misma forma que GET /consultas/{id}
    /// (afiliado y profesional incluidos): el cliente reemplaza su copia con la respuesta.
    /// </summary>
    private async Task<Consulta> CargarCompletaAsync(int id) => (await ObtenerPorIdAsync(id))!;
}
