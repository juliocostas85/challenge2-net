using ConsultasPlan.Models;

namespace ConsultasPlan.Domain;

/// <summary>
/// Error al intentar cambiar el estado de una consulta.
/// EsConflictoDeEstado: la transición no existe desde el estado actual (409).
/// Si es false, la transición existe pero no se cumple una condición de fecha (400).
/// </summary>
public record ErrorTransicion(bool EsConflictoDeEstado, string Mensaje);

/// <summary>
/// Ciclo de vida de una consulta, definido en un único lugar y aplicado por todas las
/// operaciones que cambian el estado (cancelar, atención, inasistencia y cambio genérico).
///
///   Pendiente  → Confirmado | Cancelado | Atendido | Inasistencia
///   Confirmado → Cancelado | Atendido | Inasistencia
///   Cancelado, Atendido, Inasistencia → estados finales
///
/// Condiciones de fecha:
///   Confirmado   → la consulta todavía no ocurrió.
///   Cancelado    → faltan al menos 24 h.
///   Atendido / Inasistencia → la consulta ya ocurrió.
/// </summary>
public static class ConsultaTransiciones
{
    public static readonly TimeSpan AnticipacionMinimaCancelacion = TimeSpan.FromHours(24);

    private static readonly IReadOnlyDictionary<EstadoConsulta, EstadoConsulta[]> Permitidas =
        new Dictionary<EstadoConsulta, EstadoConsulta[]>
        {
            [EstadoConsulta.Pendiente]    = [EstadoConsulta.Confirmado, EstadoConsulta.Cancelado, EstadoConsulta.Atendido, EstadoConsulta.Inasistencia],
            [EstadoConsulta.Confirmado]   = [EstadoConsulta.Cancelado, EstadoConsulta.Atendido, EstadoConsulta.Inasistencia],
            [EstadoConsulta.Cancelado]    = [],
            [EstadoConsulta.Atendido]     = [],
            [EstadoConsulta.Inasistencia] = []
        };

    public static IReadOnlyCollection<EstadoConsulta> Siguientes(EstadoConsulta actual) => Permitidas[actual];

    public static bool EsFinal(EstadoConsulta estado) => Permitidas[estado].Length == 0;

    /// <summary>
    /// Cancelación iniciada por el sistema al dar de baja al afiliado. Respeta el ciclo de vida
    /// (solo consultas Pendientes o Confirmadas que todavía no ocurrieron) pero no la anticipación
    /// de 24 h, que es una regla para las cancelaciones del afiliado.
    /// </summary>
    public static bool PuedeCancelarsePorBaja(Consulta consulta, DateTime ahora) =>
        consulta.FechaHora > ahora && Permitidas[consulta.Estado].Contains(EstadoConsulta.Cancelado);

    /// <summary>Devuelve null si la transición es válida; si no, el motivo.</summary>
    public static ErrorTransicion? Validar(Consulta consulta, EstadoConsulta destino, DateTime ahora)
    {
        if (consulta.Estado == destino)
            return new(true, $"La consulta ya se encuentra en estado {destino}.");

        if (!Permitidas[consulta.Estado].Contains(destino))
            return new(true, EsFinal(consulta.Estado)
                ? $"La consulta está en estado {consulta.Estado} y ya no admite cambios."
                : $"No se puede pasar una consulta de {consulta.Estado} a {destino}.");

        return destino switch
        {
            EstadoConsulta.Confirmado when consulta.FechaHora <= ahora =>
                new(false, "No se puede confirmar una consulta que ya ocurrió."),
            EstadoConsulta.Cancelado when consulta.FechaHora - ahora < AnticipacionMinimaCancelacion =>
                new(false, "No se puede cancelar con menos de 24 horas de anticipación."),
            EstadoConsulta.Atendido when consulta.FechaHora > ahora =>
                new(false, "No se puede registrar la atención de una consulta futura."),
            EstadoConsulta.Inasistencia when consulta.FechaHora > ahora =>
                new(false, "No se puede registrar una inasistencia sobre una consulta futura."),
            _ => null
        };
    }
}
