using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica.Web.Configuration;

/// <summary>
/// Opciones de conexión hacia la API externa de movimientos.
/// Se enlaza desde la sección "ApiSettings" de appsettings.json (Options Pattern)
/// y se valida al arrancar la aplicación, evitando fallos silenciosos en producción.
/// </summary>
public sealed class ApiSettings
{
    public const string SectionName = "ApiSettings";

    [Required(ErrorMessage = "ApiSettings:BaseUrl es obligatorio.")]
    [Url(ErrorMessage = "ApiSettings:BaseUrl debe ser una URL válida.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "ApiSettings:Endpoint es obligatorio.")]
    public string Endpoint { get; init; } = string.Empty;

    [Range(1, 120, ErrorMessage = "ApiSettings:TimeoutSeconds debe estar entre 1 y 120 segundos.")]
    public int TimeoutSeconds { get; init; } = 15;
}
