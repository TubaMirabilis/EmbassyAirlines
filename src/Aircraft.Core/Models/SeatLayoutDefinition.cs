using System.Text.Json.Serialization;

namespace Aircraft.Core.Models;

public sealed class SeatLayoutDefinition
{
    private Dictionary<RowRange, SeatSectionDefinition>? _businessRows;
    private Dictionary<RowRange, SeatSectionDefinition>? _economyRows;
    [JsonPropertyName("EquipmentType")]
    public string EquipmentType { get; init; } = null!;
    [JsonPropertyName("BusinessRows")]
    public Dictionary<string, SeatSectionDefinition> BusinessRowsRaw { get; init; } = [];
    [JsonIgnore]
    public IReadOnlyDictionary<RowRange, SeatSectionDefinition> BusinessRows => _businessRows ??=
        BusinessRowsRaw.ToDictionary(
            kvp => RowRange.Parse(kvp.Key),
            kvp => kvp.Value
        );
    [JsonPropertyName("EconomyRows")]
    public Dictionary<string, SeatSectionDefinition> EconomyRowsRaw { get; init; } = [];
    [JsonIgnore]
    public IReadOnlyDictionary<RowRange, SeatSectionDefinition> EconomyRows => _economyRows ??=
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
                var n = section.EveryNthRowOnly;
                if (n is not null && n > 0 && (row - range.Start) % n != 0)
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
