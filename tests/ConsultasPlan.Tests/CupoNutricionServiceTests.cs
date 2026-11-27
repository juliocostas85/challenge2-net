using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Services;
using ConsultasPlan.Tests.Support;

namespace ConsultasPlan.Tests;

/// <summary>
/// La política aplicada de punta a punta al crear consultas (servicio + base). Los escenarios replican
/// los afiliados del seed, que el código base trae armados para esta funcionalidad.
/// </summary>
public class CupoNutricionServiceTests : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 11, 25, 12, 0, 0);
    private static readonly DateTime EsteMes = new(2026, 11, 27, 10, 0, 0);
    private static readonly DateTime ProximoMes = new(2026, 12, 15, 10, 0, 0);

    private readonly BaseDeDatosEnMemoria _db = new();
    private readonly ConsultaService _servicio;
    private readonly Profesional _nutricion;
    private readonly Profesional _otraNutricionista;
    private readonly Profesional _cardiologia;

    public CupoNutricionServiceTests()
    {
        _servicio = Servicios.Consultas(_db, new RelojFijo(Ahora));
        _nutricion = _db.AgregarProfesional("Nutrición");
        _otraNutricionista = _db.AgregarProfesional("Nutrición");
        _cardiologia = _db.AgregarProfesional("Cardiología");
    }

    private Task<Resultado<Consulta>> Agendar(Afiliado afiliado, DateTime fecha,
        ModalidadConsulta modalidad = ModalidadConsulta.Presencial, Profesional? profesional = null) =>
        _servicio.CrearAsync(new CrearConsultaRequest
        {
            AfiliadoId = afiliado.Id, ProfesionalId = (profesional ?? _nutricion).Id,
            FechaHora = fecha, Modalidad = modalidad, Motivo = "Nutrición"
        });

    private void Historial(Afiliado afiliado, EstadoConsulta estado, params DateTime[] fechas)
    {
        foreach (var fecha in fechas) _db.AgregarConsulta(afiliado, _otraNutricionista, fecha, estado);
    }

    [Fact]
    public async Task Sin_historial_puede_agendar()
    {
        Assert.True((await Agendar(_db.AgregarAfiliado(), EsteMes)).EsOk);
    }

    [Fact]
    public async Task Con_cinco_en_el_anio_agenda_la_sexta_y_no_una_septima()
    {
        var afiliado = _db.AgregarAfiliado();
        Historial(afiliado, EstadoConsulta.Atendido, Enumerable.Range(6, 5).Select(m => new DateTime(2026, m, 10, 11, 0, 0)).ToArray());

        Assert.True((await Agendar(afiliado, EsteMes)).EsOk);
        Assert.Equal(TipoResultado.Invalido, (await Agendar(afiliado, ProximoMes)).Tipo);
    }

    [Fact]
    public async Task Con_seis_en_el_anio_no_puede_agendar()
    {
        var afiliado = _db.AgregarAfiliado();
        Historial(afiliado, EstadoConsulta.Atendido, Enumerable.Range(1, 6).Select(m => new DateTime(2026, m, 10, 10, 0, 0)).ToArray());

        var resultado = await Agendar(afiliado, EsteMes);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Contains("6 consultas", resultado.Mensaje);
    }

    [Fact]
    public async Task Con_una_en_el_mes_no_puede_otra_ese_mes_pero_si_el_siguiente()
    {
        var afiliado = _db.AgregarAfiliado();
        Historial(afiliado, EstadoConsulta.Atendido, new DateTime(2026, 11, 1, 10, 0, 0));

        Assert.Equal(TipoResultado.Invalido, (await Agendar(afiliado, EsteMes)).Tipo);
        Assert.True((await Agendar(afiliado, ProximoMes)).EsOk);
    }

    [Fact]
    public async Task Virtuales_y_canceladas_no_consumen_cupo()
    {
        var afiliado = _db.AgregarAfiliado();
        _db.AgregarConsulta(afiliado, _otraNutricionista, new DateTime(2026, 11, 3, 16, 0, 0), EstadoConsulta.Atendido, ModalidadConsulta.Virtual);
        Historial(afiliado, EstadoConsulta.Cancelado, new DateTime(2026, 11, 10, 16, 0, 0));

        Assert.True((await Agendar(afiliado, EsteMes)).EsOk);
    }

    [Fact]
    public async Task Las_virtuales_no_tienen_limite()
    {
        var afiliado = _db.AgregarAfiliado();
        Historial(afiliado, EstadoConsulta.Atendido, Enumerable.Range(1, 11).Select(m => new DateTime(2026, m, 10, 10, 0, 0)).ToArray());

        Assert.True((await Agendar(afiliado, EsteMes, ModalidadConsulta.Virtual)).EsOk);
    }

    [Fact]
    public async Task Inasistencias_y_pendientes_consumen_cupo()
    {
        var afiliado = _db.AgregarAfiliado();
        Historial(afiliado, EstadoConsulta.Inasistencia, new DateTime(2026, 11, 2, 9, 0, 0));
        Historial(afiliado, EstadoConsulta.Pendiente, new DateTime(2026, 12, 2, 15, 0, 0));

        Assert.Equal(TipoResultado.Invalido, (await Agendar(afiliado, EsteMes)).Tipo);
        Assert.Equal(TipoResultado.Invalido, (await Agendar(afiliado, ProximoMes)).Tipo);
    }

    [Fact]
    public async Task Consultas_del_anio_anterior_no_cuentan()
    {
        var afiliado = _db.AgregarAfiliado();
        Historial(afiliado, EstadoConsulta.Atendido, Enumerable.Range(7, 6).Select(m => new DateTime(2025, m, 10, 10, 0, 0)).ToArray());

        Assert.True((await Agendar(afiliado, EsteMes)).EsOk);
    }

    [Fact]
    public async Task Otras_especialidades_no_aplican_ni_consumen()
    {
        var afiliado = _db.AgregarAfiliado();
        Historial(afiliado, EstadoConsulta.Atendido, new DateTime(2026, 11, 1, 10, 0, 0));

        Assert.True((await Agendar(afiliado, EsteMes, profesional: _cardiologia)).EsOk);
    }

    [Fact]
    public async Task Cancelar_libera_el_cupo_del_mes()
    {
        var afiliado = _db.AgregarAfiliado();
        var primera = (await Agendar(afiliado, ProximoMes)).Valor!;
        await _servicio.CancelarAsync(primera.Id);

        Assert.True((await Agendar(afiliado, ProximoMes.AddDays(3))).EsOk);
    }

    [Fact]
    public async Task Estado_del_cupo_por_afiliado()
    {
        var conDos = _db.AgregarAfiliado("30000001");
        var sinNada = _db.AgregarAfiliado("30000002");
        Historial(conDos, EstadoConsulta.Atendido, new DateTime(2026, 3, 10, 10, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0));

        var estados = await Servicios.Cupo(_db, new RelojFijo(Ahora)).ObtenerEstadoActualAsync([conDos.Id, sinNada.Id]);

        Assert.Equal(4, estados[conDos.Id].Disponibles);
        Assert.True(estados[conDos.Id].UsadaEnMes);
        Assert.Equal(6, estados[sinNada.Id].Disponibles);
        Assert.False(estados[sinNada.Id].UsadaEnMes);
    }

    public void Dispose() => _db.Dispose();
}
