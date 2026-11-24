using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Services;
using ConsultasPlan.Tests.Support;

namespace ConsultasPlan.Tests;

public class ConsultaServiceTests : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 6, 15, 12, 0, 0);
    private readonly BaseDeDatosEnMemoria _db = new();
    private readonly ConsultaService _servicio;

    public ConsultaServiceTests()
    {
        _servicio = new ConsultaService(_db.Context, new RelojFijo(Ahora));
    }

    private static CrearConsultaRequest Pedido(int afiliadoId, int profesionalId, DateTime fechaHora) => new()
    {
        AfiliadoId = afiliadoId, ProfesionalId = profesionalId, FechaHora = fechaHora,
        Modalidad = ModalidadConsulta.Presencial, Motivo = "Control"
    };

    [Fact]
    public async Task Crear_consulta_futura_la_deja_pendiente()
    {
        var afiliado = _db.AgregarAfiliado();
        var profesional = _db.AgregarProfesional();

        var resultado = await _servicio.CrearAsync(Pedido(afiliado.Id, profesional.Id, Ahora.AddDays(2)));

        Assert.True(resultado.EsOk);
        Assert.Equal(EstadoConsulta.Pendiente, resultado.Valor!.Estado);
        Assert.Equal(Ahora, resultado.Valor.FechaCreacion);
    }

    [Fact]
    public async Task Crear_consulta_en_el_pasado_es_invalido()
    {
        var afiliado = _db.AgregarAfiliado();
        var profesional = _db.AgregarProfesional();

        var resultado = await _servicio.CrearAsync(Pedido(afiliado.Id, profesional.Id, Ahora.AddMinutes(-1)));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task Crear_consulta_para_afiliado_dado_de_baja_es_invalido()
    {
        var afiliado = _db.AgregarAfiliado(activo: false);
        var profesional = _db.AgregarProfesional();

        var resultado = await _servicio.CrearAsync(Pedido(afiliado.Id, profesional.Id, Ahora.AddDays(2)));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task Crear_consulta_en_horario_ocupado_del_profesional_es_invalido()
    {
        var profesional = _db.AgregarProfesional();
        _db.AgregarConsulta(_db.AgregarAfiliado("30000001"), profesional, Ahora.AddDays(2));

        var resultado = await _servicio.CrearAsync(Pedido(_db.AgregarAfiliado("30000002").Id, profesional.Id, Ahora.AddDays(2)));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task Afiliado_no_puede_tener_dos_consultas_a_la_misma_hora_con_distintos_profesionales()
    {
        var afiliado = _db.AgregarAfiliado();
        _db.AgregarConsulta(afiliado, _db.AgregarProfesional("Cardiología"), Ahora.AddDays(2));

        var resultado = await _servicio.CrearAsync(Pedido(afiliado.Id, _db.AgregarProfesional().Id, Ahora.AddDays(2)));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task Una_consulta_cancelada_libera_el_horario_del_afiliado()
    {
        var afiliado = _db.AgregarAfiliado();
        _db.AgregarConsulta(afiliado, _db.AgregarProfesional("Cardiología"), Ahora.AddDays(2), EstadoConsulta.Cancelado);

        var resultado = await _servicio.CrearAsync(Pedido(afiliado.Id, _db.AgregarProfesional().Id, Ahora.AddDays(2)));

        Assert.True(resultado.EsOk);
    }

    [Fact]
    public async Task Cambio_generico_de_estado_no_reactiva_una_consulta_cancelada()
    {
        var consulta = _db.AgregarConsulta(_db.AgregarAfiliado(), _db.AgregarProfesional(), Ahora.AddDays(5), EstadoConsulta.Cancelado);

        var resultado = await _servicio.ActualizarEstadoAsync(consulta.Id, EstadoConsulta.Pendiente);

        Assert.Equal(TipoResultado.Conflicto, resultado.Tipo);
        Assert.Equal(EstadoConsulta.Cancelado, _db.Context.Consultas.Find(consulta.Id)!.Estado);
    }

    [Fact]
    public async Task No_se_puede_cancelar_una_consulta_atendida()
    {
        var consulta = _db.AgregarConsulta(_db.AgregarAfiliado(), _db.AgregarProfesional(), Ahora.AddDays(-5), EstadoConsulta.Atendido);

        var resultado = await _servicio.CancelarAsync(consulta.Id);

        Assert.Equal(TipoResultado.Conflicto, resultado.Tipo);
    }

    [Fact]
    public async Task Registrar_atencion_de_consulta_pasada_la_marca_atendida()
    {
        var consulta = _db.AgregarConsulta(_db.AgregarAfiliado(), _db.AgregarProfesional(), Ahora.AddHours(-2), EstadoConsulta.Confirmado);

        var resultado = await _servicio.RegistrarAtencionAsync(consulta.Id);

        Assert.True(resultado.EsOk);
        Assert.Equal(EstadoConsulta.Atendido, resultado.Valor!.Estado);
    }

    [Fact]
    public async Task Crear_y_cambiar_estado_devuelven_la_consulta_completa_como_el_GET()
    {
        var afiliado = _db.AgregarAfiliado();
        var profesional = _db.AgregarProfesional();
        var creada = (await _servicio.CrearAsync(Pedido(afiliado.Id, profesional.Id, Ahora.AddDays(3)))).Valor!;
        _db.Context.ChangeTracker.Clear(); // simula un request nuevo: nada precargado en el contexto

        var confirmada = (await _servicio.ActualizarEstadoAsync(creada.Id, EstadoConsulta.Confirmado)).Valor!;

        Assert.NotNull(creada.Profesional);
        Assert.NotNull(confirmada.Afiliado);
        Assert.NotNull(confirmada.Profesional);
    }

    [Fact]
    public async Task FechaHora_con_segundos_no_esquiva_el_horario_ocupado()
    {
        var profesional = _db.AgregarProfesional();
        var turno = Ahora.AddDays(2);
        _db.AgregarConsulta(_db.AgregarAfiliado("30000001"), profesional, turno);

        var resultado = await _servicio.CrearAsync(Pedido(_db.AgregarAfiliado("30000002").Id, profesional.Id, turno.AddSeconds(30)));

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task FechaHora_con_zona_horaria_se_guarda_en_hora_local()
    {
        var argentina = TimeZoneInfo.CreateCustomTimeZone("AR", TimeSpan.FromHours(-3), "AR", "AR");
        var servicio = new ConsultaService(_db.Context, new RelojFijo(Ahora, argentina));
        var afiliado = _db.AgregarAfiliado();
        var profesional = _db.AgregarProfesional();
        var utc = new DateTime(2026, 6, 20, 13, 0, 0, DateTimeKind.Utc);

        var consulta = (await servicio.CrearAsync(Pedido(afiliado.Id, profesional.Id, utc))).Valor!;

        Assert.Equal(new DateTime(2026, 6, 20, 10, 0, 0), consulta.FechaHora);
    }

    [Fact]
    public async Task Operar_sobre_consulta_inexistente_es_no_encontrado()
    {
        Assert.Equal(TipoResultado.NoEncontrado, (await _servicio.CancelarAsync(999)).Tipo);
    }

    public void Dispose() => _db.Dispose();
}
