namespace PruebaTecnica.Web.Tests;

/// <summary>
/// <see cref="HttpMessageHandler"/> de prueba que delega en un <see cref="Func{T,TResult}"/>
/// configurable, para simular respuestas o fallos del backend sin abrir sockets reales.
/// </summary>
internal sealed class FakeHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respuesta) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => respuesta(request, cancellationToken);
}
