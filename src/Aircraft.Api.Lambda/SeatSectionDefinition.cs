using System.Text.Json.Serialization;

namespace Aircraft.Api.Lambda;

public sealed class SeatSectionDefinition
{
    [JsonPropertyName("Seats")]
    public IEnumerable<char> Seats { get; set; } = Array.Empty<char>();
    [JsonPropertyName("SeatType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SeatType SeatType { get; set; } = default!;
    [JsonPropertyName("EveryNthRowOnly")]
    public int? EveryNthRowOnly { get; set; }
    public IEnumerable<int> RowsIn(RowRange range)
    {
        if (EveryNthRowOnly is int n && n > 0)
        {
            var start = range.Start;
            foreach (var row in range)
            {
                if ((row - start) % n == 0)
                {
                    yield return row;
                }
            }
        }
        else
        {
            foreach (var row in range)
            {
                yield return row;
            }
        }
    }
}
