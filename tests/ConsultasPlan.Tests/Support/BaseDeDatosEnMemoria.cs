using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Models;

namespace ConsultasPlan.Tests.Support;

/// <summary>SQLite en memoria: mismo motor que producción (FKs, índices únicos) sin archivos.</summary>
public sealed class BaseDeDatosEnMemoria : IDisposable
{
    private readonly SqliteConnection _conexion;
    public AppDbContext Context { get; }

    public BaseDeDatosEnMemoria()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        Context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conexion).Options);
        Context.Database.EnsureCreated();
    }

    public Afiliado AgregarAfiliado(string dni = "30111222", bool activo = true, bool bloqueado = false)
    {
        var afiliado = new Afiliado { NombreCompleto = "Afiliado de prueba", DNI = dni, isActive = activo, Bloqueado = bloqueado };
        Context.Afiliados.Add(afiliado);
        Context.SaveChanges();
        return afiliado;
    }

    public Profesional AgregarProfesional(string especialidad = "Nutrición")
    {
        var sede = new Sede { Nombre = "Sede", Direccion = "Dirección" };
        Context.Sedes.Add(sede);
        Context.SaveChanges();
        var profesional = new Profesional { NombreCompleto = "Profesional de prueba", Especialidad = especialidad, SedeId = sede.Id };
        Context.Profesionales.Add(profesional);
        Context.SaveChanges();
        return profesional;
    }

    public Consulta AgregarConsulta(Afiliado afiliado, Profesional profesional, DateTime fechaHora,
        EstadoConsulta estado = EstadoConsulta.Pendiente, ModalidadConsulta modalidad = ModalidadConsulta.Presencial)
    {
        var consulta = new Consulta
        {
            AfiliadoId = afiliado.Id, ProfesionalId = profesional.Id, FechaHora = fechaHora,
            Estado = estado, Modalidad = modalidad, Motivo = "Prueba"
        };
        Context.Consultas.Add(consulta);
        Context.SaveChanges();
        return consulta;
    }

    public void Dispose()
    {
        Context.Dispose();
        _conexion.Dispose();
    }
}
