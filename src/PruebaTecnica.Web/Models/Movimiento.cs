using System.Text.Json.Serialization;

namespace PruebaTecnica.Web.Models;

/// <summary>
/// Representa un movimiento tal como lo expone la API externa.
/// Es un modelo de solo lectura: la UI únicamente lo consulta, nunca lo muta.
/// </summary>
public sealed class Movimiento
{
    [JsonPropertyName("Codigo")]
    public int Codigo { get; init; }

    [JsonPropertyName("Descripcion")]
    public string Descripcion { get; init; } = string.Empty;

    [JsonPropertyName("VActiva")]
    public bool VActiva { get; init; }
}
