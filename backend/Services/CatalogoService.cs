using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Models;

namespace ConsultasPlan.Services;

/// <summary>Datos de referencia de solo lectura (profesionales y sedes).</summary>
public interface ICatalogoService
{
    Task<List<Profesional>> ObtenerProfesionalesAsync();
    Task<List<Sede>> ObtenerSedesAsync();
}

public class CatalogoService : ICatalogoService
{
    private readonly AppDbContext _context;

    public CatalogoService(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Profesional>> ObtenerProfesionalesAsync() =>
        _context.Profesionales.AsNoTracking().Include(p => p.Sede).ToListAsync();

    public Task<List<Sede>> ObtenerSedesAsync() =>
        _context.Sedes.AsNoTracking().ToListAsync();
}
