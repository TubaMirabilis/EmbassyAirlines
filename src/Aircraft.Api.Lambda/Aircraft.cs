using Shared;

namespace Aircraft.Api.Lambda;

internal sealed class Aircraft
{
    private readonly List<Seat> _seats = [];
    private Aircraft(AircraftCreationArgs args)
    {
        Ensure.NotNullOrEmpty(args.TailNumber);
        Ensure.NotNullOrEmpty(args.EquipmentCode);
        if (string.IsNullOrWhiteSpace(args.ParkedAt) == string.IsNullOrWhiteSpace(args.EnRouteTo))
        {
            throw new ArgumentException("Aircraft must be either parked or en route, but not both.");
        }
        if (args.Status == Status.Parked && string.IsNullOrWhiteSpace(args.ParkedAt))
        {
            throw new ArgumentException("Parked aircraft must have a ParkedAt location.");
        }
        if (args.Status == Status.EnRoute && string.IsNullOrWhiteSpace(args.EnRouteTo))
        {
            throw new ArgumentException("Destination must be provided for en route aircraft.");
        }
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        TailNumber = args.TailNumber;
        EquipmentCode = args.EquipmentCode;
        DryOperatingWeight = args.DryOperatingWeight;
        MaximumTakeoffWeight = args.MaximumTakeoffWeight;
        MaximumLandingWeight = args.MaximumLandingWeight;
        MaximumZeroFuelWeight = args.MaximumZeroFuelWeight;
        MaximumFuelWeight = args.MaximumFuelWeight;
        Status = args.Status;
        ParkedAt = args.ParkedAt?.Trim().ToUpperInvariant();
        EnRouteTo = args.EnRouteTo?.Trim().ToUpperInvariant();
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
    public Status Status { get; private set; }
    public string? ParkedAt { get; private set; }
    public string? EnRouteTo { get; private set; }
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Aircraft Create(AircraftCreationArgs args) => new(args);
    public void MarkAsEnRoute(string destination)
    {
        Ensure.NotNullOrEmpty(destination);
        EnRouteTo = destination.Trim().ToUpperInvariant();
        ParkedAt = null;
        Status = Status.EnRoute;
    }
    public void MarkAsParked(string location)
    {
        Ensure.NotNullOrEmpty(location);
        ParkedAt = location.Trim().ToUpperInvariant();
        EnRouteTo = null;
        Status = Status.Parked;
    }
}
