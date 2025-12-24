using System.Text.Json.Serialization;

namespace Aircraft.Core.Models;

public sealed class SeatSectionDefinition
{
    [JsonPropertyName("Seats")]
    public IEnumerable<char> Seats { get; init; } = [];
    [JsonPropertyName("SeatType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SeatType SeatType { get; init; } = default!;
    [JsonPropertyName("EveryNthRowOnly")]
    public int? EveryNthRowOnly { get; init; }
}
