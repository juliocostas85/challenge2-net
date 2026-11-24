namespace ConsultasPlan.Tests.Support;

/// <summary>TimeProvider con hora fija para testear reglas que dependen de la fecha.</summary>
public class RelojFijo : TimeProvider
{
    private DateTimeOffset _ahora;

    public RelojFijo(DateTime ahoraLocal)
    {
        _ahora = new DateTimeOffset(ahoraLocal, TimeSpan.Zero);
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public override DateTimeOffset GetUtcNow() => _ahora;

    public void Avanzar(TimeSpan intervalo) => _ahora = _ahora.Add(intervalo);
}
