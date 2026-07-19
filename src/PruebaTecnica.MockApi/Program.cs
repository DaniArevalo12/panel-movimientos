using System.Text.Json;
using PruebaTecnica.MockApi;
using PruebaTecnica.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

var ejecutandoDetrasDeProxyExterno = app.UsarPuertoDePlataformaSiAplica();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!ejecutandoDetrasDeProxyExterno)
{
    app.UseHttpsRedirection();
}

// El sistema origen real expone las propiedades en PascalCase exacto (Codigo, Descripcion, VActiva).
// Se desactiva la política de camelCase por defecto de ASP.NET Core para reproducir ese contrato fielmente.
var contratoOrigenOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    PropertyNamingPolicy = null,
};

// Simula latencia y datos reales de un backend externo, para que el cliente
// (PruebaTecnica.Web) se pruebe contra un servicio HTTP real y no contra datos en memoria propios.
app.MapGet("/api/movimientos", async () =>
{
    await Task.Delay(Random.Shared.Next(150, 450));
    return Results.Json(MovimientosDataSource.Todos, contratoOrigenOptions);
})
.WithName("GetMovimientos");

// Endpoint de health check sin la latencia artificial de arriba: los health checks de la
// plataforma de hosting deben responder rápido y de forma predecible, no simular al backend real.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("HealthCheck")
    .ExcludeFromDescription();

app.Run();
