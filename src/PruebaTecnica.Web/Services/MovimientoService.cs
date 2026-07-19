using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
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
public sealed class MovimientoService : IMovimientoService
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
        _logger.LogInformation("Iniciando consulta de movimientos a {Endpoint}", _apiSettings.Endpoint);

        try
        {
            var movimientos = await _httpClient.GetFromJsonAsync<List<Movimiento>>(_apiSettings.Endpoint, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Consulta de movimientos completada en {ElapsedMilliseconds} ms. Registros obtenidos: {Cantidad}",
                stopwatch.ElapsedMilliseconds,
                movimientos?.Count ?? 0);

            return Result<IReadOnlyList<Movimiento>>.Success(movimientos ?? []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancelación solicitada por el llamador (navegación, Dispose de la página): no es un error de negocio.
            throw;
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Timeout al consultar movimientos tras {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "El servicio de movimientos tardó demasiado en responder. Inténtalo nuevamente.");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error de comunicación al consultar movimientos tras {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "No fue posible conectar con el servicio de movimientos. Verifica tu conexión e inténtalo de nuevo.");
        }
        catch (System.Text.Json.JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Respuesta con formato inesperado al consultar movimientos");
            return Result<IReadOnlyList<Movimiento>>.Failure(
                "El servicio de movimientos devolvió una respuesta con un formato inesperado.");
        }
    }
}
