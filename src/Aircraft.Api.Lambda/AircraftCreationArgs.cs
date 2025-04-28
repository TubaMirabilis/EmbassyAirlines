namespace Aircraft.Api.Lambda;

public sealed record AircraftCreationArgs
{
    public string? TailNumber { get; init; }
    public string? EquipmentCode { get; init; }
    public int DryOperatingWeight { get; init; }
    public int MaximumTakeoffWeight { get; init; }
    public int MaximumLandingWeight { get; init; }
    public int MaximumZeroFuelWeight { get; init; }
    public int MaximumFuelWeight { get; init; }
    public IEnumerable<Seat> Seats { get; init; } = [];
}
