namespace ConsultasPlan.Tests.Support;

/// <summary>
/// TimeProvider con hora fija para testear reglas que dependen de la fecha.
/// <paramref name="ahoraLocal"/> es la hora local en <paramref name="zona"/> (UTC por defecto).
/// </summary>
public class RelojFijo : TimeProvider
{
    private readonly TimeZoneInfo _zona;
    private DateTimeOffset _ahoraUtc;

    public RelojFijo(DateTime ahoraLocal, TimeZoneInfo? zona = null)
    {
        _zona = zona ?? TimeZoneInfo.Utc;
        var ahora = DateTime.SpecifyKind(ahoraLocal, DateTimeKind.Unspecified);
        _ahoraUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(ahora, _zona), TimeSpan.Zero);
    }

    public override TimeZoneInfo LocalTimeZone => _zona;
    public override DateTimeOffset GetUtcNow() => _ahoraUtc;

    public void Avanzar(TimeSpan intervalo) => _ahoraUtc = _ahoraUtc.Add(intervalo);
}
