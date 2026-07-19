namespace PruebaTecnica.Web.Common;

/// <summary>
/// Encapsula el resultado de una operación que puede fallar, sin recurrir a excepciones
/// para el control de flujo. Los consumidores (componentes Razor) reaccionan a un valor
/// tipado en lugar de envolver cada llamada en try/catch.
/// </summary>
/// <typeparam name="T">Tipo del valor devuelto cuando la operación tiene éxito.</typeparam>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(isSuccess: true, value, errorMessage: null);

    public static Result<T> Failure(string errorMessage) => new(isSuccess: false, value: default, errorMessage);
}
