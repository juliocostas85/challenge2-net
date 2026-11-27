using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Services;
using ConsultasPlan.Services.Agendamiento;
using ConsultasPlan.Tests.Support;

namespace ConsultasPlan.Tests;

public class ReglasAgendamientoTests : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 6, 15, 12, 0, 0);
    private readonly BaseDeDatosEnMemoria _db = new();

    private SolicitudAgendamiento Solicitud(Afiliado afiliado) =>
        new(new Consulta { AfiliadoId = afiliado.Id, FechaHora = Ahora.AddDays(1) }, afiliado, _db.AgregarProfesional());

    [Fact]
    public async Task AfiliadoHabilitado_rechaza_dados_de_baja_y_bloqueados()
    {
        var regla = new AfiliadoHabilitado();
        Assert.Null(await regla.ValidarAsync(Solicitud(_db.AgregarAfiliado("30000001"))));
        Assert.NotNull(await regla.ValidarAsync(Solicitud(_db.AgregarAfiliado("30000002", activo: false))));
        Assert.NotNull(await regla.ValidarAsync(Solicitud(_db.AgregarAfiliado("30000003", bloqueado: true))));
    }

    /// <summary>Extensión sin modificar ConsultaService (OCP): una regla nueva se suma a la lista.</summary>
    private class SinConsultasLosDomingos : IReglaAgendamiento
    {
        public Task<string?> ValidarAsync(SolicitudAgendamiento s) =>
            Task.FromResult(s.Consulta.FechaHora.DayOfWeek == DayOfWeek.Sunday ? "No se atiende los domingos." : null);
    }

    [Fact]
    public async Task Una_regla_nueva_se_aplica_sin_modificar_el_servicio()
    {
        var servicio = new ConsultaService(_db.Context, new RelojFijo(Ahora),
            [.. Servicios.Reglas(_db), new SinConsultasLosDomingos()]);
        var afiliado = _db.AgregarAfiliado();
        var domingo = new DateTime(2026, 6, 21, 10, 0, 0);

        var resultado = await servicio.CrearAsync(new CrearConsultaRequest
        {
            AfiliadoId = afiliado.Id, ProfesionalId = _db.AgregarProfesional().Id,
            FechaHora = domingo, Modalidad = ModalidadConsulta.Presencial
        });

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Equal("No se atiende los domingos.", resultado.Mensaje);
    }

    public void Dispose() => _db.Dispose();
}
