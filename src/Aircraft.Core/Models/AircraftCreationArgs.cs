namespace Aircraft.Core.Models;

public sealed record AircraftCreationArgs
{
    public string? TailNumber { get; init; }
    public string? EquipmentCode { get; init; }
    public required Weight DryOperatingWeight { get; init; }
    public required Weight MaximumTakeoffWeight { get; init; }
    public required Weight MaximumLandingWeight { get; init; }
    public required Weight MaximumZeroFuelWeight { get; init; }
    public required Weight MaximumFuelWeight { get; init; }
    public required AircraftLocationData AircraftLocationData { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required SeatLayoutDefinition Seats { get; init; }
}
