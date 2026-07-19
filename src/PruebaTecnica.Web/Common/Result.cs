using System.Diagnostics.CodeAnalysis;

namespace PruebaTecnica.Web.Common;

/// <summary>
/// Encapsula el resultado de una operación que puede fallar, sin recurrir a excepciones
/// para el control de flujo. Los consumidores (componentes Razor) reaccionan a un valor
/// tipado en lugar de envolver cada llamada en try/catch.
/// </summary>
/// <typeparam name="T">Tipo del valor devuelto cuando la operación tiene éxito.</typeparam>
public sealed class Result<T>
{
    /// <summary>Indica si la operación se completó correctamente.</summary>
    public bool IsSuccess { get; }

    /// <summary>Valor obtenido cuando <see cref="IsSuccess"/> es <c>true</c>; <c>default</c> en caso contrario.</summary>
    public T? Value { get; }

    /// <summary>Mensaje de error legible por el usuario cuando <see cref="IsSuccess"/> es <c>false</c>; <c>null</c> en caso contrario.</summary>
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    /// <summary>Crea un resultado exitoso con el valor obtenido.</summary>
    [SuppressMessage(
        "Design",
        "CA1000:No declarar miembros estáticos en tipos genéricos",
        Justification = "El fix recomendado (mover a una clase estática no genérica 'Result' con métodos " +
            "genéricos) no elimina la fricción real: Failure<T>(string) no puede inferir T de un string, " +
            "así que el llamador igual debe especificar el tipo explícitamente. El costo de la migración " +
            "no se paga con ningún beneficio de ergonomía en el caso de fallo.")]
    public static Result<T> Success(T value) => new(isSuccess: true, value, errorMessage: null);

    /// <summary>Crea un resultado fallido con un mensaje de error legible por el usuario.</summary>
    [SuppressMessage(
        "Design",
        "CA1000:No declarar miembros estáticos en tipos genéricos",
        Justification = "Ver justificación en Success(T).")]
    public static Result<T> Failure(string errorMessage) => new(isSuccess: false, value: default, errorMessage);
}
