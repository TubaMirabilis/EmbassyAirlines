using System.Text.Json.Serialization;

namespace Aircraft.Api.Lambda;

public sealed class Aircraft
{
    private Aircraft(string tailNumber, string equipmentCode, int dryOperatingWeight, int maximumTakeoffWeight, int maximumLandingWeight, int maximumZeroFuelWeight, int maximumFuelWeight)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        TailNumber = tailNumber;
        EquipmentCode = equipmentCode;
        DryOperatingWeight = dryOperatingWeight;
        MaximumTakeoffWeight = maximumTakeoffWeight;
        MaximumLandingWeight = maximumLandingWeight;
        MaximumZeroFuelWeight = maximumZeroFuelWeight;
        MaximumFuelWeight = maximumFuelWeight;
    }
#pragma warning disable CS8618
    [JsonConstructor]
    private Aircraft()
    {
    }
#pragma warning restore CS8618
    [JsonInclude]
    public Guid Id { get; init; }
    [JsonInclude]
    public DateTime CreatedAt { get; init; }
    [JsonInclude]
    public DateTime UpdatedAt { get; private set; }
    [JsonInclude]
    public string TailNumber { get; private set; }
    [JsonInclude]
    public string EquipmentCode { get; private set; }
    [JsonInclude]
    public int DryOperatingWeight { get; private set; }
    [JsonInclude]
    public int MaximumTakeoffWeight { get; private set; }
    [JsonInclude]
    public int MaximumLandingWeight { get; private set; }
    [JsonInclude]
    public int MaximumZeroFuelWeight { get; private set; }
    [JsonInclude]
    public int MaximumFuelWeight { get; private set; }
    public static Aircraft Create(string tailNumber, string equipmentCode, int dryOperatingWeight, int maximumTakeoffWeight, int maximumLandingWeight, int maximumZeroFuelWeight, int maximumFuelWeight) => new(tailNumber, equipmentCode, dryOperatingWeight, maximumTakeoffWeight, maximumLandingWeight, maximumZeroFuelWeight, maximumFuelWeight);
    public void Update(string tailNumber, string equipmentCode, int dryOperatingWeight, int maximumTakeoffWeight, int maximumLandingWeight, int maximumZeroFuelWeight, int maximumFuelWeight)
    {
        TailNumber = tailNumber;
        EquipmentCode = equipmentCode;
        DryOperatingWeight = dryOperatingWeight;
        MaximumTakeoffWeight = maximumTakeoffWeight;
        MaximumLandingWeight = maximumLandingWeight;
        MaximumZeroFuelWeight = maximumZeroFuelWeight;
        MaximumFuelWeight = maximumFuelWeight;
        UpdatedAt = DateTime.UtcNow;
    }
}
