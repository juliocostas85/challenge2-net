using Microsoft.Extensions.Options;
using ConsultasPlan.Domain;
using ConsultasPlan.Services;
using ConsultasPlan.Services.Agendamiento;

namespace ConsultasPlan.Tests.Support;

/// <summary>Arma los servicios reales sobre la base en memoria, con las mismas reglas que registra Program.cs.</summary>
public static class Servicios
{
    public static CupoNutricionService Cupo(BaseDeDatosEnMemoria db, TimeProvider reloj, CupoNutricionOptions? opciones = null) =>
        new(db.Context, reloj, Options.Create(opciones ?? new CupoNutricionOptions()));

    public static IReglaAgendamiento[] Reglas(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
    [
        new AfiliadoHabilitado(),
        new HorarioProfesionalDisponible(db.Context),
        new HorarioAfiliadoDisponible(db.Context),
        Cupo(db, reloj)
    ];

    public static ConsultaService Consultas(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
        new(db.Context, reloj, Reglas(db, reloj));

    public static AfiliadoService Afiliados(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
        new(db.Context, reloj, Consultas(db, reloj));
}
