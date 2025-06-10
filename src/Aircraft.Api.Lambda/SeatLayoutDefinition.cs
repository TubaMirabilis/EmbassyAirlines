using System.Text.Json.Serialization;

namespace Aircraft.Api.Lambda;

public sealed class SeatLayoutDefinition
{
    [JsonPropertyName("EquipmentType")]
    public string EquipmentType { get; set; } = default!;
    [JsonPropertyName("BusinessRows")]
    public Dictionary<string, List<SeatSectionDefinition>> BusinessRowsRaw { get; } = new Dictionary<string, List<SeatSectionDefinition>>();
    [JsonIgnore]
    public Dictionary<RowRange, List<SeatSectionDefinition>> BusinessRows =>
        BusinessRowsRaw.ToDictionary(
            kvp => RowRange.Parse(kvp.Key),
            kvp => kvp.Value
        );
    [JsonPropertyName("EconomyRows")]
    public Dictionary<string, SeatSectionDefinition> EconomyRowsRaw { get; } = new Dictionary<string, SeatSectionDefinition>();
    [JsonIgnore]
    public Dictionary<RowRange, SeatSectionDefinition> EconomyRows =>
        EconomyRowsRaw.ToDictionary(
            kvp => RowRange.Parse(kvp.Key),
            kvp => kvp.Value
        );
    public IEnumerable<Seat> ToSeatsCollection(Guid aircraftId)
    {
        var business = BusinessRows.SelectMany(kvp => kvp.Value.Select(section => (Range: kvp.Key, Section: section)));
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
