using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Services;
using ConsultasPlan.Tests.Support;

namespace ConsultasPlan.Tests;

public class AfiliadoServiceTests : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 6, 15, 12, 0, 0);
    private readonly BaseDeDatosEnMemoria _db = new();
    private readonly AfiliadoService _servicio;

    public AfiliadoServiceTests()
    {
        _servicio = new AfiliadoService(_db.Context, new RelojFijo(Ahora));
    }

    private static AfiliadoRequest Pedido(string dni) => new()
    {
        NombreCompleto = "Nuevo Afiliado", DNI = dni, Email = "nuevo@example.com", Telefono = "1100000000"
    };

    [Fact]
    public async Task Eliminar_es_baja_logica_y_conserva_el_historial()
    {
        var afiliado = _db.AgregarAfiliado();
        var consulta = _db.AgregarConsulta(afiliado, _db.AgregarProfesional(), new DateTime(2026, 5, 10, 10, 0, 0));

        var resultado = await _servicio.EliminarAsync(afiliado.Id);

        Assert.True(resultado.EsOk);
        Assert.False(_db.Context.Afiliados.Find(afiliado.Id)!.isActive);
        Assert.Equal(afiliado.Id, _db.Context.Consultas.Find(consulta.Id)!.AfiliadoId);
        Assert.DoesNotContain(await _servicio.ObtenerTodosAsync(), a => a.Id == afiliado.Id);
    }

    [Fact]
    public async Task Baja_cancela_consultas_futuras_activas_incluso_dentro_de_las_24_horas()
    {
        var afiliado = _db.AgregarAfiliado();
        var profesional = _db.AgregarProfesional();
        var pendiente = _db.AgregarConsulta(afiliado, profesional, Ahora.AddDays(10), EstadoConsulta.Pendiente);
        var confirmadaManiana = _db.AgregarConsulta(afiliado, profesional, Ahora.AddHours(3), EstadoConsulta.Confirmado);

        await _servicio.EliminarAsync(afiliado.Id);

        Assert.Equal(EstadoConsulta.Cancelado, _db.Context.Consultas.Find(pendiente.Id)!.Estado);
        Assert.Equal(EstadoConsulta.Cancelado, _db.Context.Consultas.Find(confirmadaManiana.Id)!.Estado);
    }

    [Fact]
    public async Task Baja_no_modifica_consultas_pasadas_ni_finales()
    {
        var afiliado = _db.AgregarAfiliado();
        var profesional = _db.AgregarProfesional();
        var atendida = _db.AgregarConsulta(afiliado, profesional, Ahora.AddDays(-10), EstadoConsulta.Atendido);
        var pendientePasada = _db.AgregarConsulta(afiliado, profesional, Ahora.AddHours(-2), EstadoConsulta.Pendiente);

        await _servicio.EliminarAsync(afiliado.Id);

        Assert.Equal(EstadoConsulta.Atendido, _db.Context.Consultas.Find(atendida.Id)!.Estado);
        // Ya ocurrió: queda para registrar atención o inasistencia.
        Assert.Equal(EstadoConsulta.Pendiente, _db.Context.Consultas.Find(pendientePasada.Id)!.Estado);
    }

    [Fact]
    public async Task Baja_no_afecta_consultas_de_otros_afiliados()
    {
        var profesional = _db.AgregarProfesional();
        var otra = _db.AgregarConsulta(_db.AgregarAfiliado("30000009"), profesional, Ahora.AddDays(5));

        await _servicio.EliminarAsync(_db.AgregarAfiliado("30000010").Id);

        Assert.Equal(EstadoConsulta.Pendiente, _db.Context.Consultas.Find(otra.Id)!.Estado);
    }

    [Fact]
    public async Task No_permite_crear_dos_afiliados_con_el_mismo_DNI()
    {
        _db.AgregarAfiliado("30111222");

        var resultado = await _servicio.CrearAsync(Pedido("30111222"));

        Assert.Equal(TipoResultado.Conflicto, resultado.Tipo);
    }

    [Fact]
    public async Task No_permite_recrear_un_afiliado_dado_de_baja()
    {
        // Si se permitiera, el afiliado "nuevo" empezaría con el historial vacío.
        _db.AgregarAfiliado("30111222", activo: false);

        var resultado = await _servicio.CrearAsync(Pedido("30111222"));

        Assert.Equal(TipoResultado.Conflicto, resultado.Tipo);
    }

    [Fact]
    public async Task Crear_ignora_campos_gestionados_por_el_sistema()
    {
        var resultado = await _servicio.CrearAsync(Pedido("30999888"));

        Assert.True(resultado.EsOk);
        Assert.True(resultado.Valor!.isActive);
        Assert.False(resultado.Valor.Bloqueado);
        Assert.Equal(0, resultado.Valor.ConsultasUsadas);
    }

    public void Dispose() => _db.Dispose();
}
