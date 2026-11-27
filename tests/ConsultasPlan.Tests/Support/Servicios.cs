using Microsoft.Extensions.Options;
using ConsultasPlan.Domain;
using ConsultasPlan.Services;
using ConsultasPlan.Services.Agendamiento;

namespace ConsultasPlan.Tests.Support;

/// <summary>Arma los servicios reales sobre la base en memoria, con las mismas reglas que registra Program.cs.</summary>
public static class Servicios
{
    public static CupoNutricionService Cupo(BaseDeDatosEnMemoria db, CupoNutricionOptions? opciones = null) =>
        new(db.Context, Options.Create(opciones ?? new CupoNutricionOptions()));

    public static IReglaAgendamiento[] Reglas(BaseDeDatosEnMemoria db) =>
    [
        new AfiliadoHabilitado(),
        new HorarioProfesionalDisponible(db.Context),
        new HorarioAfiliadoDisponible(db.Context)
    ];

    public static ConsultaService Consultas(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
        new(db.Context, reloj, Cupo(db), Reglas(db));

    public static AfiliadoService Afiliados(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
        new(db.Context, reloj, Cupo(db), Consultas(db, reloj));
}
