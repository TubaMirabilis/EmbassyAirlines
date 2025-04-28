using Shared;

namespace Aircraft.Api.Lambda;

public sealed class Aircraft
{
    private readonly List<Seat> _seats = [];
    private Aircraft(AircraftCreationArgs args)
    {
        Ensure.NotNullOrEmpty(args.TailNumber);
        Ensure.NotNullOrEmpty(args.EquipmentCode);
        Ensure.GreaterThanZero(args.DryOperatingWeight);
        Ensure.GreaterThanZero(args.MaximumTakeoffWeight);
        Ensure.GreaterThanZero(args.MaximumLandingWeight);
        Ensure.GreaterThanZero(args.MaximumZeroFuelWeight);
        Ensure.GreaterThanZero(args.MaximumFuelWeight);
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        TailNumber = args.TailNumber;
        EquipmentCode = args.EquipmentCode;
        DryOperatingWeight = args.DryOperatingWeight;
        MaximumTakeoffWeight = args.MaximumTakeoffWeight;
        MaximumLandingWeight = args.MaximumLandingWeight;
        MaximumZeroFuelWeight = args.MaximumZeroFuelWeight;
        MaximumFuelWeight = args.MaximumFuelWeight;
        _seats.AddRange(args.Seats);
    }
#pragma warning disable CS8618
    private Aircraft()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TailNumber { get; private set; }
    public string EquipmentCode { get; private set; }
    public int DryOperatingWeight { get; private set; }
    public int MaximumTakeoffWeight { get; private set; }
    public int MaximumLandingWeight { get; private set; }
    public int MaximumZeroFuelWeight { get; private set; }
    public int MaximumFuelWeight { get; private set; }
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Aircraft Create(AircraftCreationArgs args) => new(args);
}
