using Shared;

namespace Aircraft.Api.Lambda;

internal sealed class Aircraft
{
    private readonly List<Seat> _seats = [];
    private Aircraft(AircraftCreationArgs args)
    {
        Ensure.NotNullOrEmpty(args.TailNumber);
        Ensure.NotNullOrEmpty(args.EquipmentCode);
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        TailNumber = args.TailNumber;
        EquipmentCode = args.EquipmentCode;
        DryOperatingWeight = args.DryOperatingWeight;
        MaximumTakeoffWeight = args.MaximumTakeoffWeight;
        MaximumLandingWeight = args.MaximumLandingWeight;
        MaximumZeroFuelWeight = args.MaximumZeroFuelWeight;
        MaximumFuelWeight = args.MaximumFuelWeight;
        var seats = args.Seats.ToSeatsCollection(Id).ToList();
        var seen = new HashSet<(byte RowNumber, char Letter)>();
        if (seats.Any(seat => !seen.Add((seat.RowNumber, seat.Letter))))
        {
            throw new ArgumentException("Duplicate seat definitions found in the seat layout.");
        }
        _seats.AddRange(seats);
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
    public Weight DryOperatingWeight { get; private set; }
    public Weight MaximumTakeoffWeight { get; private set; }
    public Weight MaximumLandingWeight { get; private set; }
    public Weight MaximumZeroFuelWeight { get; private set; }
    public Weight MaximumFuelWeight { get; private set; }
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Aircraft Create(AircraftCreationArgs args) => new(args);
}
