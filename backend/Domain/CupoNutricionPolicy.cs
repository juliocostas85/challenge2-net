using System.Globalization;
using System.Text;
using ConsultasPlan.Models;

namespace ConsultasPlan.Domain;

/// <summary>Parámetros de la política (sección "CupoNutricion" de appsettings.json).</summary>
public class CupoNutricionOptions
{
    public const string Seccion = "CupoNutricion";

    /// <summary>Especialidad del profesional que identifica una consulta de nutrición.</summary>
    public string Especialidad { get; set; } = "Nutrición";
    public int MaximoAnual { get; set; } = 6;
    public int MaximoMensual { get; set; } = 1;
}

/// <summary>Situación del cupo de un afiliado en un año calendario.</summary>
public record EstadoCupo(int Anio, int MaximoAnual, int Usadas, int Disponibles, bool UsadaEnMes);

/// <summary>
/// Política de cupo: "el plan incluye 6 consultas de nutrición por año, no más de 1 por mes.
/// Las consultas no usadas no se acumulan. Aplica a consultas presenciales."
///
/// Reglas puras (sin base de datos ni reloj) para poder testearlas en forma aislada:
///  - Aplica solo a consultas Presenciales con un profesional de Nutrición.
///  - Consume cupo toda consulta no cancelada: Pendiente, Confirmado, Atendido e Inasistencia.
///  - Año y mes son de calendario y se toman de la fecha de la consulta (no de la reserva).
///  - Cada año y cada mes se evalúan por separado: lo no usado no se acumula.
/// </summary>
public static class CupoNutricionPolicy
{
    private static readonly CultureInfo Cultura = new("es-AR");

    public static bool EsNutricion(string? especialidad, CupoNutricionOptions opciones) =>
        Normalizar(especialidad) == Normalizar(opciones.Especialidad);

    public static bool Aplica(ModalidadConsulta modalidad, string? especialidad, CupoNutricionOptions opciones) =>
        modalidad == ModalidadConsulta.Presencial && EsNutricion(especialidad, opciones);

    public static bool ConsumeCupo(EstadoConsulta estado) => estado != EstadoConsulta.Cancelado;

    /// <summary>
    /// Valida si se puede agendar una consulta en <paramref name="fechaNueva"/>, dadas las fechas de las
    /// consultas que ya consumen cupo. Devuelve null si está permitido; si no, el motivo.
    /// </summary>
    public static string? Validar(IEnumerable<DateTime> fechasQueConsumen, DateTime fechaNueva, CupoNutricionOptions opciones)
    {
        var delAnio = fechasQueConsumen.Where(f => f.Year == fechaNueva.Year).ToList();

        if (delAnio.Count >= opciones.MaximoAnual)
            return $"El afiliado ya utilizó las {opciones.MaximoAnual} consultas presenciales de nutrición " +
                   $"que cubre el plan en {fechaNueva.Year}.";

        if (delAnio.Count(f => f.Month == fechaNueva.Month) >= opciones.MaximoMensual)
            return $"El afiliado ya tiene una consulta presencial de nutrición en " +
                   $"{Cultura.DateTimeFormat.GetMonthName(fechaNueva.Month)} de {fechaNueva.Year}. " +
                   $"El plan cubre como máximo {opciones.MaximoMensual} por mes.";

        return null;
    }

    /// <summary>Estado del cupo en el año de <paramref name="referencia"/> (y su mes, para "usada en el mes").</summary>
    public static EstadoCupo Calcular(IEnumerable<DateTime> fechasQueConsumen, DateTime referencia, CupoNutricionOptions opciones)
    {
        var delAnio = fechasQueConsumen.Where(f => f.Year == referencia.Year).ToList();
        var usadas = Math.Min(delAnio.Count, opciones.MaximoAnual);
        return new EstadoCupo(
            referencia.Year,
            opciones.MaximoAnual,
            usadas,
            opciones.MaximoAnual - usadas,
            delAnio.Count(f => f.Month == referencia.Month) >= opciones.MaximoMensual);
    }

    /// <summary>Compara especialidades sin distinguir mayúsculas ni tildes ("Nutrición" = "nutricion").</summary>
    private static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var sinTildes = new StringBuilder();
        foreach (var c in texto.Trim().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sinTildes.Append(c);
        return sinTildes.ToString().ToUpperInvariant();
    }
}
