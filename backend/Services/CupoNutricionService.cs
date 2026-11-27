using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ConsultasPlan.Data;
using ConsultasPlan.Domain;
using ConsultasPlan.Models;
using ConsultasPlan.Services.Agendamiento;

namespace ConsultasPlan.Services;

/// <summary>Consulta del cupo de nutrición (la usan las respuestas de afiliados).</summary>
public interface IConsultaCupoNutricion
{
    /// <summary>Estado del cupo de cada afiliado pedido, en el año y mes actuales.</summary>
    Task<IReadOnlyDictionary<int, EstadoCupo>> ObtenerEstadoActualAsync(IReadOnlyCollection<int> afiliadoIds);
}

/// <summary>
/// Aplica la política de cupo de nutrición (Domain/CupoNutricionPolicy) sobre el historial guardado.
/// Expone dos interfaces separadas según quién la usa (ISP):
///  - IReglaAgendamiento: ConsultaService la aplica al agendar, como cualquier otra regla.
///  - IConsultaCupoNutricion: el listado de afiliados informa el cupo disponible.
/// </summary>
public class CupoNutricionService : IReglaAgendamiento, IConsultaCupoNutricion
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _tiempo;
    private readonly CupoNutricionOptions _opciones;

    public CupoNutricionService(AppDbContext context, TimeProvider tiempo, IOptions<CupoNutricionOptions> opciones)
    {
        _context = context;
        _tiempo = tiempo;
        _opciones = opciones.Value;
    }

    /// <summary>
    /// Se valida solo al agendar: el ciclo de vida no permite reactivar una consulta cancelada,
    /// así que ninguna otra operación puede aumentar el consumo.
    /// </summary>
    public async Task<string?> ValidarAsync(SolicitudAgendamiento solicitud)
    {
        var consulta = solicitud.Consulta;
        if (!CupoNutricionPolicy.Aplica(consulta.Modalidad, solicitud.Profesional.Especialidad, _opciones))
            return null;

        var fechas = await FechasQueConsumenAsync([consulta.AfiliadoId], consulta.FechaHora.Year);
        return CupoNutricionPolicy.Validar(fechas.Select(f => f.FechaHora), consulta.FechaHora, _opciones);
    }

    public async Task<IReadOnlyDictionary<int, EstadoCupo>> ObtenerEstadoActualAsync(IReadOnlyCollection<int> afiliadoIds)
    {
        var ahora = _tiempo.AhoraLocal();
        var porAfiliado = (await FechasQueConsumenAsync(afiliadoIds, ahora.Year)).ToLookup(f => f.AfiliadoId, f => f.FechaHora);
        return afiliadoIds.ToDictionary(id => id, id => CupoNutricionPolicy.Calcular(porAfiliado[id], ahora, _opciones));
    }

    /// <summary>
    /// Consultas presenciales no canceladas del año, de los afiliados pedidos. El filtro por especialidad se
    /// hace en memoria porque compara sin tildes ni mayúsculas (SQLite no lo resuelve en la consulta).
    /// </summary>
    private async Task<List<(int AfiliadoId, DateTime FechaHora)>> FechasQueConsumenAsync(IReadOnlyCollection<int> afiliadoIds, int anio)
    {
        var desde = new DateTime(anio, 1, 1);
        var hasta = desde.AddYears(1);

        var candidatas = await _context.Consultas
            .AsNoTracking()
            .Where(c => afiliadoIds.Contains(c.AfiliadoId)
                        && c.Modalidad == ModalidadConsulta.Presencial
                        && c.Estado != EstadoConsulta.Cancelado
                        && c.FechaHora >= desde && c.FechaHora < hasta)
            .Select(c => new { c.AfiliadoId, c.FechaHora, c.Estado, c.Profesional!.Especialidad })
            .ToListAsync();

        return candidatas
            .Where(c => CupoNutricionPolicy.ConsumeCupo(c.Estado) && CupoNutricionPolicy.EsNutricion(c.Especialidad, _opciones))
            .Select(c => (c.AfiliadoId, c.FechaHora))
            .ToList();
    }
}
