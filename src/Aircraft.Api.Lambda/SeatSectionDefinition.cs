using System.Text.Json.Serialization;

namespace Aircraft.Api.Lambda;

internal sealed class SeatSectionDefinition
{
    [JsonPropertyName("Seats")]
    public IEnumerable<char> Seats { get; set; } = Array.Empty<char>();
    [JsonPropertyName("SeatType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SeatType SeatType { get; set; } = default!;
    [JsonPropertyName("EveryNthRowOnly")]
    public int? EveryNthRowOnly { get; set; }
}
