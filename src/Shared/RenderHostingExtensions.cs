namespace PruebaTecnica.Shared;

/// <summary>
/// Ajustes de hosting compartidos por ambos servicios (Web y MockApi) para plataformas
/// tipo Render/Heroku, que asignan el puerto dinámicamente vía la variable de entorno
/// <c>PORT</c> y terminan TLS en su propio borde, reenviando tráfico HTTP plano al contenedor.
/// Vive como archivo enlazado (no como proyecto propio) porque es infraestructura de
/// arranque de ~10 líneas: crear un tercer proyecto solo para esto sería sobreingeniería.
/// </summary>
internal static class RenderHostingExtensions
{
    /// <summary>
    /// Si la plataforma inyectó la variable <c>PORT</c>, reconfigura la app para escuchar
    /// en ese puerto sobre HTTP plano (el TLS ya lo termina el proxy de la plataforma).
    /// </summary>
    /// <returns><c>true</c> si la app está corriendo detrás de ese proxy externo.</returns>
    public static bool UsarPuertoDePlataformaSiAplica(this WebApplication app)
    {
        var puertoAsignadoPorPlataforma = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrEmpty(puertoAsignadoPorPlataforma))
        {
            return false;
        }

        app.Urls.Clear();
        app.Urls.Add($"http://0.0.0.0:{puertoAsignadoPorPlataforma}");
        return true;
    }
}
