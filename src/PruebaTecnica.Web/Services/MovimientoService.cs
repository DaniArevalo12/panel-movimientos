using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using PruebaTecnica.Web.Common;
using PruebaTecnica.Web.Configuration;
using PruebaTecnica.Web.Models;

namespace PruebaTecnica.Web.Services;

/// <summary>
/// Implementación de <see cref="IMovimientoService"/> basada en <see cref="HttpClient"/>.
/// El cliente se registra vía HttpClientFactory (ver Program.cs); esta clase nunca
/// instancia HttpClient manualmente ni conoce detalles de resiliencia (retries/timeouts),
/// que se configuran de forma transversal en la composición raíz.
/// </summary>
public sealed partial class MovimientoService : IMovimientoService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<MovimientoService> _logger;

    public MovimientoService(HttpClient httpClient, IOptions<ApiSettings> apiSettings, ILogger<MovimientoService> logger)
    {
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Movimiento>>> ObtenerMovimientosAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        LogIniciandoConsulta(_apiSettings.Endpoint);

        try
        {
            // El endpoint se combina con una BaseAddress terminada en "/" (ver Program.cs) sin
            // barra inicial propia, para que la resolución de URI relativa lo anexe en vez de
            // reemplazar un posible prefijo de ruta de la BaseUrl.
            var movimientos = await _httpClient.GetFromJsonAsync<List<Movimiento>>(
                _apiSettings.Endpoint.TrimStart('/'), cancellationToken);

            LogConsultaCompletada(stopwatch.ElapsedMilliseconds, movimientos?.Count ?? 0);
            return Result<IReadOnlyList<Movimiento>>.Success(movimientos ?? []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelación solicitada por el llamador (navegación, Dispose de la página): no es un error de negocio.
            throw;
        }
        catch (TaskCanceledException ex)
        {
            LogTimeout(ex, stopwatch.ElapsedMilliseconds);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "El servicio de movimientos tardó demasiado en responder. Inténtalo nuevamente.");
        }
        catch (TimeoutRejectedException ex)
        {
            // Lanzada por la estrategia de timeout de Microsoft.Extensions.Http.Resilience
            // (AttemptTimeout/TotalRequestTimeout) cuando el timeout lo impone Polly en vez del propio HttpClient.
            LogTimeoutResiliencia(ex, stopwatch.ElapsedMilliseconds);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "El servicio de movimientos tardó demasiado en responder. Inténtalo nuevamente.");
        }
        catch (BrokenCircuitException ex)
        {
            // El circuit breaker está abierto tras fallos repetidos: se evita golpear un backend
            // que ya sabemos que está caído, en vez de dejar que la excepción llegue a la UI.
            LogCircuitoAbierto(ex, stopwatch.ElapsedMilliseconds);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "El servicio de movimientos no está disponible en este momento. Inténtalo más tarde.");
        }
        catch (HttpRequestException ex)
        {
            LogErrorComunicacion(ex, stopwatch.ElapsedMilliseconds);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "No fue posible conectar con el servicio de movimientos. Verifica tu conexión e inténtalo de nuevo.");
        }
        catch (JsonException ex)
        {
            LogFormatoInesperado(ex);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "El servicio de movimientos devolvió una respuesta con un formato inesperado.");
        }
        catch (Exception ex)
        {
            // Red de seguridad: ningún fallo inesperado debe escapar del servicio y tumbar el circuito de Blazor.
            // Se loggea con el detalle completo; el usuario solo ve un mensaje genérico y accionable.
            LogErrorInesperado(ex, stopwatch.ElapsedMilliseconds);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "Ocurrió un error inesperado al consultar los movimientos. Inténtalo nuevamente.");
        }
    }

    // Logging de alto rendimiento (CA1848): estos métodos se generan en tiempo de compilación
    // y evitan las asignaciones de boxing/params array de las llamadas clásicas a ILogger.LogXxx.

    [LoggerMessage(Level = LogLevel.Information, Message = "Iniciando consulta de movimientos a {Endpoint}")]
    private partial void LogIniciandoConsulta(string endpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Consulta de movimientos completada en {ElapsedMilliseconds} ms. Registros obtenidos: {Cantidad}")]
    private partial void LogConsultaCompletada(long elapsedMilliseconds, int cantidad);

    [LoggerMessage(Level = LogLevel.Error, Message = "Timeout al consultar movimientos tras {ElapsedMilliseconds} ms")]
    private partial void LogTimeout(Exception ex, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Timeout de resiliencia al consultar movimientos tras {ElapsedMilliseconds} ms")]
    private partial void LogTimeoutResiliencia(Exception ex, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Circuit breaker abierto al consultar movimientos tras {ElapsedMilliseconds} ms")]
    private partial void LogCircuitoAbierto(Exception ex, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error de comunicación al consultar movimientos tras {ElapsedMilliseconds} ms")]
    private partial void LogErrorComunicacion(Exception ex, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "Respuesta con formato inesperado al consultar movimientos")]
    private partial void LogFormatoInesperado(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error inesperado al consultar movimientos tras {ElapsedMilliseconds} ms")]
    private partial void LogErrorInesperado(Exception ex, long elapsedMilliseconds);
}
