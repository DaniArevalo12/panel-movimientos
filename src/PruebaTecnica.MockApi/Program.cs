using System.Text.Json;
using PruebaTecnica.MockApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

// Plataformas tipo Render/Heroku asignan el puerto dinámicamente vía la variable PORT
// y terminan TLS en su propio borde, reenviando tráfico HTTP plano al contenedor.
var puertoAsignadoPorPlataforma = Environment.GetEnvironmentVariable("PORT");
var ejecutandoDetrasDeProxyExterno = !string.IsNullOrEmpty(puertoAsignadoPorPlataforma);
if (ejecutandoDetrasDeProxyExterno)
{
    app.Urls.Clear();
    app.Urls.Add($"http://0.0.0.0:{puertoAsignadoPorPlataforma}");
}

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

app.Run();
