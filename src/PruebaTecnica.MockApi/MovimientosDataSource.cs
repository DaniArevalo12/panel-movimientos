namespace PruebaTecnica.MockApi;

/// <summary>
/// Origen de datos estático que simula el catálogo de movimientos de un ERP.
/// Reemplaza esto por el backend real cuando la API oficial de la prueba técnica esté disponible.
/// </summary>
internal static class MovimientosDataSource
{
    public static readonly IReadOnlyList<MovimientoDto> Todos =
    [
        new(29, "Ajuste al Inventario", false),
        new(51, "Avance Produccion", false),
        new(12, "Entrada por Compra", true),
        new(15, "Salida por Venta", true),
        new(8, "Traspaso entre Bodegas", true),
        new(33, "Devolucion de Cliente", true),
        new(44, "Devolucion a Proveedor", false),
        new(19, "Consumo de Materia Prima", true),
        new(27, "Merma de Inventario", false),
        new(3, "Entrada por Produccion Terminada", true),
        new(61, "Reproceso de Producto", false),
        new(72, "Ajuste por Auditoria", false),
        new(5, "Traslado a Consignacion", true),
        new(91, "Recepcion de Importacion", true),
        new(38, "Salida por Muestra Gratis", false),
        new(46, "Entrada por Donacion", false),
        new(17, "Salida por Obsolescencia", false),
        new(24, "Reserva de Pedido", true),
        new(58, "Liberacion de Reserva", true),
        new(66, "Cierre de Orden de Produccion", true),
    ];
}
