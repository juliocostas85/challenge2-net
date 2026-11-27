using ConsultasPlan.Services;
using ConsultasPlan.Services.Agendamiento;

namespace ConsultasPlan.Tests.Support;

/// <summary>Arma los servicios reales sobre la base en memoria, con las mismas reglas que registra Program.cs.</summary>
public static class Servicios
{
    public static IReglaAgendamiento[] Reglas(BaseDeDatosEnMemoria db) =>
    [
        new AfiliadoHabilitado(),
        new HorarioProfesionalDisponible(db.Context),
        new HorarioAfiliadoDisponible(db.Context)
    ];

    public static ConsultaService Consultas(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
        new(db.Context, reloj, Reglas(db));

    public static AfiliadoService Afiliados(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
        new(db.Context, reloj);
}
