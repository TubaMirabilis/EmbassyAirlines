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
}
