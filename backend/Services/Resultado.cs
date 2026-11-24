namespace ConsultasPlan.Services;

public enum TipoResultado
{
    Ok,
    NoEncontrado,
    Invalido,
    Conflicto
}

/// <summary>
/// Resultado de una operación de negocio. Los servicios no conocen HTTP:
/// el controlador traduce el tipo de resultado al código de estado correspondiente.
/// </summary>
public class Resultado<T>
{
    public TipoResultado Tipo { get; }
    public T? Valor { get; }
    public string? Mensaje { get; }

    private Resultado(TipoResultado tipo, T? valor, string? mensaje)
    {
        Tipo = tipo;
        Valor = valor;
        Mensaje = mensaje;
    }

    public bool EsOk => Tipo == TipoResultado.Ok;

    public static Resultado<T> Ok(T valor) => new(TipoResultado.Ok, valor, null);
    public static Resultado<T> NoEncontrado(string? mensaje = null) => new(TipoResultado.NoEncontrado, default, mensaje);
    public static Resultado<T> Invalido(string mensaje) => new(TipoResultado.Invalido, default, mensaje);
    public static Resultado<T> Conflicto(string mensaje) => new(TipoResultado.Conflicto, default, mensaje);
}
