using Microsoft.Extensions.Options;
using ConsultasPlan.Domain;
using ConsultasPlan.Services;

namespace ConsultasPlan.Tests.Support;

/// <summary>Arma los servicios reales sobre la base en memoria, con la configuración por defecto del cupo.</summary>
public static class Servicios
{
    public static CupoNutricionService Cupo(BaseDeDatosEnMemoria db, CupoNutricionOptions? opciones = null) =>
        new(db.Context, Options.Create(opciones ?? new CupoNutricionOptions()));

    public static ConsultaService Consultas(BaseDeDatosEnMemoria db, TimeProvider reloj) =>
        new(db.Context, reloj, Cupo(db));
}
