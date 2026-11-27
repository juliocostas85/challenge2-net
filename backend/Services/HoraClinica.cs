namespace ConsultasPlan.Services;

public static class HoraClinica
{
    /// <summary>
    /// Hora local de la clínica: única convención de fechas del sistema. Las fechas de consulta llegan
    /// desde el frontend como hora local (input datetime-local, sin zona), por lo que todas las
    /// comparaciones se hacen en hora local.
    /// </summary>
    public static DateTime AhoraLocal(this TimeProvider tiempo) => tiempo.GetLocalNow().DateTime;
}
