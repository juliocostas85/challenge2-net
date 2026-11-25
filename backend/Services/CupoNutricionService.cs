using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ConsultasPlan.Data;
using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Services;

/// <summary>Aplica la política de cupo de nutrición sobre el historial de consultas guardado.</summary>
public interface ICupoNutricionService
{
    bool Aplica(ModalidadConsulta modalidad, Profesional profesional);

    /// <summary>Null si el afiliado puede agendar una consulta de nutrición presencial en esa fecha; si no, el motivo.</summary>
    Task<string?> ValidarAsync(int afiliadoId, DateTime fechaHora);

    /// <summary>Estado del cupo de varios afiliados en el año y mes de <paramref name="referencia"/>.</summary>
    Task<IReadOnlyDictionary<int, EstadoCupo>> ObtenerEstadoAsync(IReadOnlyCollection<int> afiliadoIds, DateTime referencia);
}

public class CupoNutricionService : ICupoNutricionService
{
    private readonly AppDbContext _context;
    private readonly CupoNutricionOptions _opciones;

    public CupoNutricionService(AppDbContext context, IOptions<CupoNutricionOptions> opciones)
    {
        _context = context;
        _opciones = opciones.Value;
    }

    public bool Aplica(ModalidadConsulta modalidad, Profesional profesional) =>
        CupoNutricionPolicy.Aplica(modalidad, profesional.Especialidad, _opciones);

    public async Task<string?> ValidarAsync(int afiliadoId, DateTime fechaHora)
    {
        var fechas = await FechasQueConsumenAsync([afiliadoId], fechaHora.Year);
        return CupoNutricionPolicy.Validar(fechas.Select(f => f.FechaHora), fechaHora, _opciones);
    }

    public async Task<IReadOnlyDictionary<int, EstadoCupo>> ObtenerEstadoAsync(IReadOnlyCollection<int> afiliadoIds, DateTime referencia)
    {
        var porAfiliado = (await FechasQueConsumenAsync(afiliadoIds, referencia.Year)).ToLookup(f => f.AfiliadoId, f => f.FechaHora);
        return afiliadoIds.ToDictionary(id => id, id => CupoNutricionPolicy.Calcular(porAfiliado[id], referencia, _opciones));
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
