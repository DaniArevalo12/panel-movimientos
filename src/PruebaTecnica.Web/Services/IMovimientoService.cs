using PruebaTecnica.Web.Common;
using PruebaTecnica.Web.Models;

namespace PruebaTecnica.Web.Services;

/// <summary>
/// Contrato para la obtención de movimientos desde el sistema de origen.
/// Vive en la capa de servicios para que los componentes Razor dependan de una
/// abstracción (DIP) y no de detalles de transporte HTTP.
/// </summary>
public interface IMovimientoService
{
    /// <summary>
    /// Obtiene el catálogo completo de movimientos.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar la operación (p. ej. al navegar fuera de la página).</param>
    /// <returns>Un <see cref="Result{T}"/> con la lista de movimientos o un mensaje de error legible.</returns>
    Task<Result<IReadOnlyList<Movimiento>>> ObtenerMovimientosAsync(CancellationToken cancellationToken = default);
}
