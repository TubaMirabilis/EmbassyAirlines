using System.Text.Json.Serialization;

namespace Aircraft.Api.Lambda;

public sealed class SeatSectionDefinition
{
    [JsonPropertyName("Seats")]
    public IEnumerable<char> Seats { get; set; } = Array.Empty<char>();
    [JsonPropertyName("SeatType")]
    public SeatType SeatType { get; set; } = default!;
    [JsonPropertyName("EveryNthRowOnly")]
    public int? EveryNthRowOnly { get; set; }
}
