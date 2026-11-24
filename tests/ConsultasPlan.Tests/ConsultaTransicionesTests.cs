using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Tests;

public class ConsultaTransicionesTests
{
    private static readonly DateTime Ahora = new(2026, 6, 15, 12, 0, 0);

    private static Consulta ConsultaEn(EstadoConsulta estado, DateTime fechaHora) =>
        new() { Estado = estado, FechaHora = fechaHora };

    [Theory]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Confirmado)]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Cancelado)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Cancelado)]
    public void Transiciones_de_consulta_futura_son_validas(EstadoConsulta origen, EstadoConsulta destino)
    {
        var consulta = ConsultaEn(origen, Ahora.AddDays(3));
        Assert.Null(ConsultaTransiciones.Validar(consulta, destino, Ahora));
    }

    [Theory]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Pendiente, EstadoConsulta.Inasistencia)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Inasistencia)]
    public void Transiciones_de_consulta_pasada_son_validas(EstadoConsulta origen, EstadoConsulta destino)
    {
        var consulta = ConsultaEn(origen, Ahora.AddHours(-1));
        Assert.Null(ConsultaTransiciones.Validar(consulta, destino, Ahora));
    }

    [Theory]
    [InlineData(EstadoConsulta.Cancelado, EstadoConsulta.Pendiente)]
    [InlineData(EstadoConsulta.Cancelado, EstadoConsulta.Confirmado)]
    [InlineData(EstadoConsulta.Atendido, EstadoConsulta.Cancelado)]
    [InlineData(EstadoConsulta.Atendido, EstadoConsulta.Inasistencia)]
    [InlineData(EstadoConsulta.Inasistencia, EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Confirmado, EstadoConsulta.Pendiente)]
    public void Transiciones_inexistentes_son_conflicto(EstadoConsulta origen, EstadoConsulta destino)
    {
        var error = ConsultaTransiciones.Validar(ConsultaEn(origen, Ahora.AddDays(3)), destino, Ahora);
        Assert.NotNull(error);
        Assert.True(error!.EsConflictoDeEstado);
    }

    [Fact]
    public void Mismo_estado_es_conflicto()
    {
        var error = ConsultaTransiciones.Validar(ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddDays(3)), EstadoConsulta.Pendiente, Ahora);
        Assert.True(error!.EsConflictoDeEstado);
    }

    [Fact]
    public void Cancelar_con_menos_de_24_horas_es_invalido()
    {
        var error = ConsultaTransiciones.Validar(ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddHours(23).AddMinutes(59)), EstadoConsulta.Cancelado, Ahora);
        Assert.NotNull(error);
        Assert.False(error!.EsConflictoDeEstado);
    }

    [Fact]
    public void Cancelar_con_exactamente_24_horas_es_valido()
    {
        Assert.Null(ConsultaTransiciones.Validar(ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddHours(24)), EstadoConsulta.Cancelado, Ahora));
    }

    [Theory]
    [InlineData(EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Inasistencia)]
    public void Atencion_o_inasistencia_de_consulta_futura_es_invalida(EstadoConsulta destino)
    {
        var error = ConsultaTransiciones.Validar(ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddMinutes(1)), destino, Ahora);
        Assert.NotNull(error);
        Assert.False(error!.EsConflictoDeEstado);
    }

    [Fact]
    public void Atencion_de_consulta_del_anio_anterior_es_valida()
    {
        // Antes se rechazaba por "año en curso": una consulta del 30/12 no podía registrarse el 2/1.
        var consulta = ConsultaEn(EstadoConsulta.Confirmado, new DateTime(2025, 12, 30, 18, 0, 0));
        Assert.Null(ConsultaTransiciones.Validar(consulta, EstadoConsulta.Atendido, new DateTime(2026, 1, 2, 9, 0, 0)));
    }

    [Fact]
    public void Confirmar_consulta_pasada_es_invalido()
    {
        var error = ConsultaTransiciones.Validar(ConsultaEn(EstadoConsulta.Pendiente, Ahora.AddHours(-1)), EstadoConsulta.Confirmado, Ahora);
        Assert.False(error!.EsConflictoDeEstado);
    }
}
