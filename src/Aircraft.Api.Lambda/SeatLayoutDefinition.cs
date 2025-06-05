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
    public IEnumerable<Seat> ToSeatsCollection()
    {
        var seats = new List<Seat>();
        foreach (var kvp in BusinessRows)
        {
            var range = kvp.Key;
            var sections = kvp.Value;
            foreach (var row in range)
            {
                foreach (var section in sections)
                {
                    var includeThisSection = section.EveryNthRowOnly is not int n || n <= 0 || (row - range.Start) % n == 0;
                    if (!includeThisSection)
                    {
                        continue;
                    }
                    foreach (var letter in section.Seats)
                    {
                        seats.Add(new Seat
                        {
                            Id = Guid.NewGuid(),
                            CreatedAt = DateTime.UtcNow,
                            RowNumber = (byte)row,
                            Letter = letter,
                            Type = section.SeatType
                        });
                    }
                }
            }
        }
        foreach (var kvp in EconomyRows)
        {
            var range = kvp.Key;
            var section = kvp.Value;
            foreach (var row in range)
            {
                var includeThisSection = section.EveryNthRowOnly is not int n || n <= 0 || (row - range.Start) % n == 0;
                if (!includeThisSection)
                {
                    continue;
                }
                foreach (var letter in section.Seats)
                {
                    seats.Add(new Seat
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        RowNumber = (byte)row,
                        Letter = letter,
                        Type = section.SeatType
                    });
                }
            }
        }
        return seats;
    }
}
