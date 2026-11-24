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

    public Task<List<Afiliado>> ObtenerTodosAsync() => _context.Afiliados.ToListAsync();

    public async Task<Afiliado?> ObtenerPorIdAsync(int id) => await _context.Afiliados.FindAsync(id);

    public async Task<Resultado<Afiliado>> CrearAsync(AfiliadoRequest request)
    {
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

        existing.NombreCompleto = datos.NombreCompleto.Trim();
        existing.DNI = datos.DNI.Trim();
        existing.Email = datos.Email.Trim();
        existing.Telefono = datos.Telefono.Trim();

        await _context.SaveChangesAsync();
        return Resultado<Afiliado>.Ok(existing);
    }

    public async Task<Resultado<Afiliado>> EliminarAsync(int id)
    {
        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null) return Resultado<Afiliado>.NoEncontrado();

        _context.Afiliados.Remove(afiliado);
        await _context.SaveChangesAsync();
        return Resultado<Afiliado>.Ok(afiliado);
    }
}
