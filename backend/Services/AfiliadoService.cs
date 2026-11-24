using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;

namespace ConsultasPlan.Services;

public interface IAfiliadoService
{
    Task<List<Afiliado>> ObtenerTodosAsync();
    Task<Afiliado?> ObtenerPorIdAsync(int id);
    Task<Resultado<Afiliado>> CrearAsync(AfiliadoRequest request);
    Task<Resultado<Afiliado>> ActualizarAsync(int id, AfiliadoRequest datos);
    Task<Resultado<Afiliado>> EliminarAsync(int id);
}

public class AfiliadoService : IAfiliadoService
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _tiempo;

    public AfiliadoService(AppDbContext context, TimeProvider tiempo)
    {
        _context = context;
        _tiempo = tiempo;
    }

    /// <summary>
    /// Hora local de la clínica. Las fechas de consulta llegan desde el frontend como hora local
    /// (input datetime-local, sin zona), por lo que todas las comparaciones se hacen en hora local.
    /// </summary>
    private DateTime Ahora => _tiempo.GetLocalNow().DateTime;

    /// <summary>Solo afiliados activos. Los dados de baja se conservan para preservar su historial.</summary>
    public Task<List<Afiliado>> ObtenerTodosAsync() =>
        _context.Afiliados.Where(a => a.isActive).ToListAsync();

    public async Task<Afiliado?> ObtenerPorIdAsync(int id) => await _context.Afiliados.FindAsync(id);

    public async Task<Resultado<Afiliado>> CrearAsync(AfiliadoRequest request)
    {
        var errorDni = await ValidarDniDisponibleAsync(request.DNI.Trim(), idExcluido: null);
        if (errorDni != null) return Resultado<Afiliado>.Conflicto(errorDni);

        var afiliado = new Afiliado
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            DNI = request.DNI.Trim(),
            Email = request.Email.Trim(),
            Telefono = request.Telefono.Trim(),
            createdAt = Ahora,
            isActive = true
        };
        _context.Afiliados.Add(afiliado);
        await _context.SaveChangesAsync();
        return Resultado<Afiliado>.Ok(afiliado);
    }

    public async Task<Resultado<Afiliado>> ActualizarAsync(int id, AfiliadoRequest datos)
    {
        var existing = await _context.Afiliados.FindAsync(id);
        if (existing == null) return Resultado<Afiliado>.NoEncontrado();
        if (!existing.isActive)
            return Resultado<Afiliado>.Conflicto("El afiliado está dado de baja y no puede modificarse.");

        var errorDni = await ValidarDniDisponibleAsync(datos.DNI.Trim(), idExcluido: id);
        if (errorDni != null) return Resultado<Afiliado>.Conflicto(errorDni);

        existing.NombreCompleto = datos.NombreCompleto.Trim();
        existing.DNI = datos.DNI.Trim();
        existing.Email = datos.Email.Trim();
        existing.Telefono = datos.Telefono.Trim();

        await _context.SaveChangesAsync();
        return Resultado<Afiliado>.Ok(existing);
    }

    /// <summary>
    /// Baja lógica: el afiliado deja de listarse y no puede agendar, pero se conservan él y su
    /// historial de consultas, del que dependen reglas como el cupo anual.
    /// </summary>
    public async Task<Resultado<Afiliado>> EliminarAsync(int id)
    {
        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null) return Resultado<Afiliado>.NoEncontrado();

        afiliado.isActive = false;
        await _context.SaveChangesAsync();
        return Resultado<Afiliado>.Ok(afiliado);
    }

    private async Task<string?> ValidarDniDisponibleAsync(string dni, int? idExcluido)
    {
        var existente = await _context.Afiliados
            .FirstOrDefaultAsync(a => a.DNI == dni && (idExcluido == null || a.Id != idExcluido));
        if (existente == null) return null;
        return existente.isActive
            ? "Ya existe un afiliado con ese DNI."
            : "Ya existe un afiliado dado de baja con ese DNI.";
    }
}
