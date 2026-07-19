using Microsoft.Extensions.Options;
using PruebaTecnica.Web.Components;
using PruebaTecnica.Web.Configuration;
using PruebaTecnica.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Options Pattern: la configuración de la API se valida al iniciar la aplicación,
// de modo que un appsettings.json mal configurado falla rápido en el arranque
// en lugar de producir errores confusos en tiempo de ejecución.
builder.Services
    .AddOptions<ApiSettings>()
    .Bind(builder.Configuration.GetSection(ApiSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// HttpClientFactory + resiliencia estándar (retry con backoff exponencial, circuit breaker
// y timeout por intento) para el cliente tipado de movimientos. Nunca se instancia HttpClient
// manualmente. El timeout por intento se alinea con ApiSettings:TimeoutSeconds.
var apiSettingsAtStartup = builder.Configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>()
    ?? new ApiSettings();

builder.Services.AddHttpClient<IMovimientoService, MovimientoService>((serviceProvider, client) =>
{
    var apiSettings = serviceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;
    client.BaseAddress = new Uri(apiSettings.BaseUrl);
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(apiSettingsAtStartup.TimeoutSeconds);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(apiSettingsAtStartup.TimeoutSeconds * 2);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(apiSettingsAtStartup.TimeoutSeconds * 2);
});

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!ejecutandoDetrasDeProxyExterno)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
