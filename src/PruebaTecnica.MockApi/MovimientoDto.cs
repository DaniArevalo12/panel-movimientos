namespace PruebaTecnica.MockApi;

/// <summary>
/// Contrato de datos expuesto por la API de movimientos.
/// Los nombres de propiedad respetan el formato exacto entregado por el sistema origen (PascalCase).
/// </summary>
internal sealed record MovimientoDto(int Codigo, string Descripcion, bool VActiva);
