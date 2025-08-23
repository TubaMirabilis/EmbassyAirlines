using System.Text.Json.Serialization;

namespace Aircraft.Api.Lambda;

public sealed class SeatLayoutDefinition
{
    [JsonPropertyName("EquipmentType")]
    public string EquipmentType { get; set; } = default!;
    [JsonPropertyName("BusinessRows")]
    [JsonInclude]
    public Dictionary<string, SeatSectionDefinition> BusinessRowsRaw { get; init; } = [];
    [JsonIgnore]
    public Dictionary<RowRange, SeatSectionDefinition> BusinessRows =>
        BusinessRowsRaw.ToDictionary(
            kvp => RowRange.Parse(kvp.Key),
            kvp => kvp.Value
        );
    [JsonPropertyName("EconomyRows")]
    [JsonInclude]
    public Dictionary<string, SeatSectionDefinition> EconomyRowsRaw { get; init; } = [];
    [JsonIgnore]
    public Dictionary<RowRange, SeatSectionDefinition> EconomyRows =>
        EconomyRowsRaw.ToDictionary(
            kvp => RowRange.Parse(kvp.Key),
            kvp => kvp.Value
        );
    public IEnumerable<Seat> ToSeatsCollection(Guid aircraftId)
    {
        var now = DateTime.UtcNow;
        var business = BusinessRows
            .Select(kvp => (Range: kvp.Key, Section: kvp.Value));
        var economy = EconomyRows
            .Select(kvp => (Range: kvp.Key, Section: kvp.Value));
        return business.Concat(economy)
            .SelectMany(x => x.Section.RowsIn(x.Range)
                .SelectMany(row => x.Section.Seats.Select(letter => new Seat
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = now,
                    RowNumber = (byte)row,     // consider checked cast if row might exceed 255
                    Letter = letter,
                    Type = x.Section.SeatType,
                    AircraftId = aircraftId
                })));
    }
}
