using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using PruebaTecnica.Web.Configuration;
using PruebaTecnica.Web.Services;

namespace PruebaTecnica.Web.Tests.Services;

public class MovimientoServiceTests
{
    private static MovimientoService CrearServicio(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respuesta)
    {
        var handler = new FakeHttpMessageHandler(respuesta);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://mock-api.test/") };
        var apiSettings = Options.Create(new ApiSettings
        {
            BaseUrl = "http://mock-api.test/",
            Endpoint = "/api/movimientos",
            TimeoutSeconds = 15,
        });

        return new MovimientoService(httpClient, apiSettings, NullLogger<MovimientoService>.Instance);
    }

    private static HttpResponseMessage RespuestaJson(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoLaApiResponde200_DevuelveMovimientosMapeadosCorrectamente()
    {
        const string json = """
            [
                { "Codigo": 29, "Descripcion": "Ajuste al Inventario", "VActiva": false },
                { "Codigo": 12, "Descripcion": "Entrada por Compra", "VActiva": true }
            ]
            """;
        var servicio = CrearServicio((_, _) => Task.FromResult(RespuestaJson(HttpStatusCode.OK, json)));

        var resultado = await servicio.ObtenerMovimientosAsync();

        Assert.True(resultado.IsSuccess);
        Assert.Null(resultado.ErrorMessage);
        Assert.NotNull(resultado.Value);
        Assert.Equal(2, resultado.Value.Count);
        Assert.Equal(29, resultado.Value[0].Codigo);
        Assert.Equal("Ajuste al Inventario", resultado.Value[0].Descripcion);
        Assert.False(resultado.Value[0].VActiva);
        Assert.True(resultado.Value[1].VActiva);
    }

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoLaApiDevuelveListaVacia_DevuelveResultadoExitosoSinRegistros()
    {
        var servicio = CrearServicio((_, _) => Task.FromResult(RespuestaJson(HttpStatusCode.OK, "[]")));

        var resultado = await servicio.ObtenerMovimientosAsync();

        Assert.True(resultado.IsSuccess);
        Assert.NotNull(resultado.Value);
        Assert.Empty(resultado.Value);
    }

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoFallaLaConexion_DevuelveResultadoFallidoConMensajeAmigable()
    {
        var servicio = CrearServicio((_, _) => throw new HttpRequestException("connection refused"));

        var resultado = await servicio.ObtenerMovimientosAsync();

        Assert.False(resultado.IsSuccess);
        Assert.Null(resultado.Value);
        Assert.Contains("conectar", resultado.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoLaRespuestaNoEsJsonValido_DevuelveResultadoFallido()
    {
        var servicio = CrearServicio((_, _) =>
            Task.FromResult(RespuestaJson(HttpStatusCode.OK, "esto no es json")));

        var resultado = await servicio.ObtenerMovimientosAsync();

        Assert.False(resultado.IsSuccess);
        Assert.Contains("formato inesperado", resultado.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoHayTimeout_DevuelveResultadoFallidoConMensajeDeTimeout()
    {
        // Se lanza sin ligar la excepción al token del llamador: así se simula un timeout
        // propio del handler (p. ej. HttpClient.Timeout), no una cancelación solicitada por quien llama.
        var servicio = CrearServicio((_, _) => throw new TaskCanceledException("timeout"));

        var resultado = await servicio.ObtenerMovimientosAsync(CancellationToken.None);

        Assert.False(resultado.IsSuccess);
        Assert.Contains("tardó demasiado", resultado.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoElLlamadorCancela_PropagaLaCancelacionEnVezDeDevolverUnResultado()
    {
        var servicio = CrearServicio(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return RespuestaJson(HttpStatusCode.OK, "[]");
        });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => servicio.ObtenerMovimientosAsync(cts.Token));
    }

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoElCircuitBreakerEstaAbierto_DevuelveResultadoFallido()
    {
        var servicio = CrearServicio((_, _) => throw new BrokenCircuitException("circuit is open"));

        var resultado = await servicio.ObtenerMovimientosAsync();

        Assert.False(resultado.IsSuccess);
        Assert.Contains("no está disponible", resultado.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ObtenerMovimientosAsync_CuandoOcurreUnaExcepcionNoAnticipada_NoSePropagaYDevuelveResultadoFallido()
    {
        // Red de seguridad: ningún tipo de excepción, ni siquiera uno no anticipado, debe escapar del servicio.
        var servicio = CrearServicio((_, _) => throw new InvalidOperationException("boom"));

        var resultado = await servicio.ObtenerMovimientosAsync();

        Assert.False(resultado.IsSuccess);
        Assert.NotNull(resultado.ErrorMessage);
    }
}
