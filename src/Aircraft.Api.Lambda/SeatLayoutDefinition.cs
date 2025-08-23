using System.Text.Json.Serialization;

namespace Aircraft.Api.Lambda;

public sealed class SeatLayoutDefinition
{
    [JsonPropertyName("EquipmentType")]
    public string EquipmentType { get; set; } = default!;
    [JsonPropertyName("BusinessRows")]
    public Dictionary<string, SeatSectionDefinition> BusinessRowsRaw { get; init; } = [];
    [JsonIgnore]
    public Dictionary<RowRange, SeatSectionDefinition> BusinessRows =>
        BusinessRowsRaw.ToDictionary(
            kvp => RowRange.Parse(kvp.Key),
            kvp => kvp.Value
        );
    [JsonPropertyName("EconomyRows")]
    public Dictionary<string, SeatSectionDefinition> EconomyRowsRaw { get; init; } = [];
    [JsonIgnore]
    public Dictionary<RowRange, SeatSectionDefinition> EconomyRows =>
        EconomyRowsRaw.ToDictionary(
            kvp => RowRange.Parse(kvp.Key),
            kvp => kvp.Value
        );
    public IEnumerable<Seat> ToSeatsCollection(Guid aircraftId)
    {
        var business = BusinessRows.Select(kvp => (Range: kvp.Key, Section: kvp.Value));
        var economy = EconomyRows.Select(kvp => (Range: kvp.Key, Section: kvp.Value));
        var all = business.Concat(economy);
        foreach (var (range, section) in all)
        {
            foreach (var row in range)
            {
                if (section.EveryNthRowOnly is int n && n > 0 && (row - range.Start) % n != 0)
                {
                    continue;
                }
                foreach (var letter in section.Seats)
                {
                    yield return new Seat
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        RowNumber = (byte)row,
                        Letter = letter,
                        Type = section.SeatType,
                        AircraftId = aircraftId
                    };
                }
            }
        }
    }
}
