using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using PruebaTecnica.Shared;
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
//
// AddStandardResilienceHandler necesita este valor de forma síncrona, antes de que el
// pipeline de validación de IOptions (ValidateOnStart) se ejecute. Por eso se valida aquí
// explícitamente con las mismas DataAnnotations que ApiSettings declara: así una
// configuración inválida falla igual de rápido y con el mismo mensaje, en vez de colarse
// sin validar hasta este punto.
var apiSettingsAtStartup = builder.Configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>()
    ?? new ApiSettings();
Validator.ValidateObject(apiSettingsAtStartup, new ValidationContext(apiSettingsAtStartup), validateAllProperties: true);

builder.Services.AddHttpClient<IMovimientoService, MovimientoService>((serviceProvider, client) =>
{
    var apiSettings = serviceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;

    // Se fuerza una barra final en la BaseAddress: la resolución de URI relativa (RFC 3986)
    // reemplaza el último segmento de ruta de la base si el endpoint no es tratado como
    // subruta. Sin esto, una BaseUrl con un prefijo de ruta (p. ej. "https://api.acme.com/erp")
    // perdería silenciosamente ese prefijo al combinarse con el endpoint.
    client.BaseAddress = new Uri(apiSettings.BaseUrl.TrimEnd('/') + "/");
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(apiSettingsAtStartup.TimeoutSeconds);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(apiSettingsAtStartup.TimeoutSeconds * 2);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(apiSettingsAtStartup.TimeoutSeconds * 2);
});

var app = builder.Build();

var ejecutandoDetrasDeProxyExterno = app.UsarPuertoDePlataformaSiAplica();

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

// Cabeceras de seguridad de bajo riesgo (no requieren ajustar CSP con nonce, que sería
// necesario por el script inline de selección de tema en App.razor): mitigan MIME-sniffing,
// filtrado de referrer y clickjacking sin condicionar el comportamiento de la SPA.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
