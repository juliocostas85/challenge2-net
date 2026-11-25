using ConsultasPlan.Domain;
using ConsultasPlan.Models;

namespace ConsultasPlan.Tests;

public class CupoNutricionPolicyTests
{
    private static readonly CupoNutricionOptions Opciones = new();

    private static DateTime Dia(int anio, int mes, int dia = 10) => new(anio, mes, dia, 10, 0, 0);

    [Theory]
    [InlineData(ModalidadConsulta.Presencial, "Nutrición", true)]
    [InlineData(ModalidadConsulta.Presencial, "nutricion", true)]
    [InlineData(ModalidadConsulta.Presencial, " NUTRICIÓN ", true)]
    [InlineData(ModalidadConsulta.Virtual, "Nutrición", false)]
    [InlineData(ModalidadConsulta.Presencial, "Cardiología", false)]
    [InlineData(ModalidadConsulta.Presencial, null, false)]
    public void Aplica_solo_a_nutricion_presencial(ModalidadConsulta modalidad, string? especialidad, bool esperado) =>
        Assert.Equal(esperado, CupoNutricionPolicy.Aplica(modalidad, especialidad, Opciones));

    [Theory]
    [InlineData(EstadoConsulta.Pendiente, true)]
    [InlineData(EstadoConsulta.Confirmado, true)]
    [InlineData(EstadoConsulta.Atendido, true)]
    [InlineData(EstadoConsulta.Inasistencia, true)]
    [InlineData(EstadoConsulta.Cancelado, false)]
    public void Consumen_cupo_todas_salvo_las_canceladas(EstadoConsulta estado, bool esperado) =>
        Assert.Equal(esperado, CupoNutricionPolicy.ConsumeCupo(estado));

    [Fact]
    public void Sin_historial_se_permite()
    {
        Assert.Null(CupoNutricionPolicy.Validar([], Dia(2026, 11), Opciones));
    }

    [Fact]
    public void Con_5_en_el_anio_se_permite_la_sexta()
    {
        var historial = Enumerable.Range(6, 5).Select(m => Dia(2026, m)); // junio a octubre
        Assert.Null(CupoNutricionPolicy.Validar(historial, Dia(2026, 11), Opciones));
    }

    [Fact]
    public void Con_6_en_el_anio_se_rechaza_por_cupo_anual()
    {
        var historial = Enumerable.Range(1, 6).Select(m => Dia(2026, m)); // enero a junio
        var error = CupoNutricionPolicy.Validar(historial, Dia(2026, 11), Opciones);
        Assert.NotNull(error);
        Assert.Contains("6 consultas", error);
    }

    [Fact]
    public void Una_en_el_mismo_mes_se_rechaza_por_cupo_mensual()
    {
        var error = CupoNutricionPolicy.Validar([Dia(2026, 11, 1)], Dia(2026, 11, 28), Opciones);
        Assert.NotNull(error);
        Assert.Contains("noviembre", error);
    }

    [Fact]
    public void Una_en_el_mes_anterior_no_impide_el_mes_siguiente()
    {
        Assert.Null(CupoNutricionPolicy.Validar([Dia(2026, 10, 31)], Dia(2026, 11, 1), Opciones));
    }

    [Fact]
    public void Consultas_del_anio_anterior_no_cuentan_ni_se_acumulan()
    {
        // 6 consultas en 2025 no limitan 2026; tampoco un 2025 sin usar da más cupo en 2026.
        var historial = Enumerable.Range(7, 6).Select(m => Dia(2025, m));
        Assert.Null(CupoNutricionPolicy.Validar(historial, Dia(2026, 1), Opciones));
    }

    [Fact]
    public void El_cupo_se_evalua_por_el_anio_de_la_consulta_no_de_la_reserva()
    {
        // Reservar en diciembre 2026 una consulta para enero 2027 usa el cupo de 2027.
        var historial = Enumerable.Range(7, 6).Select(m => Dia(2026, m));
        Assert.Null(CupoNutricionPolicy.Validar(historial, Dia(2027, 1), Opciones));
    }

    [Fact]
    public void Meses_sin_usar_no_habilitan_dos_en_el_mismo_mes()
    {
        // Sin consultas en todo el año, igual se permite solo 1 en diciembre.
        Assert.NotNull(CupoNutricionPolicy.Validar([Dia(2026, 12, 2)], Dia(2026, 12, 20), Opciones));
    }

    [Fact]
    public void Calcular_informa_usadas_disponibles_y_mes()
    {
        var historial = new[] { Dia(2026, 6), Dia(2026, 11, 1), Dia(2025, 12) };
        var estado = CupoNutricionPolicy.Calcular(historial, Dia(2026, 11, 25), Opciones);
        Assert.Equal(new EstadoCupo(2026, 6, 2, 4, true), estado);
    }

    [Fact]
    public void Limites_configurables()
    {
        var opciones = new CupoNutricionOptions { MaximoAnual = 2, MaximoMensual = 2 };
        Assert.Null(CupoNutricionPolicy.Validar([Dia(2026, 3, 1)], Dia(2026, 3, 20), opciones));
        Assert.NotNull(CupoNutricionPolicy.Validar([Dia(2026, 3, 1), Dia(2026, 3, 2)], Dia(2026, 4, 1), opciones));
    }
}
