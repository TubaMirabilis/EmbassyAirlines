using Shared;
using Shared.Contracts;

namespace Aircraft.Core.Models;

public sealed class Aircraft : Entity
{
    private readonly List<CompletedFlight> _completedFlights = [];
    private readonly List<Seat> _seats = [];
    private Aircraft(AircraftCreationArgs args)
    {
        Ensure.NotNullOrEmpty(args.TailNumber);
        Ensure.NotNullOrEmpty(args.EquipmentCode);
        if (args.Situation.EffectiveAt != args.CreatedAt)
        {
            throw new ArgumentException("The initial aircraft situation must be effective at the aircraft's creation time.", nameof(args));
        }
        Id = Guid.NewGuid();
        CreatedAt = args.CreatedAt;
        UpdatedAt = CreatedAt;
        TailNumber = args.TailNumber;
        EquipmentCode = args.EquipmentCode;
        DryOperatingWeight = args.DryOperatingWeight;
        MaximumTakeoffWeight = args.MaximumTakeoffWeight;
        MaximumLandingWeight = args.MaximumLandingWeight;
        MaximumZeroFuelWeight = args.MaximumZeroFuelWeight;
        MaximumFuelWeight = args.MaximumFuelWeight;
        Situation = args.Situation;
        InitialBlockTime = args.InitialBlockTime;
        var seats = args.Seats.ToSeatsCollection(Id, args.CreatedAt).ToList();
        var seen = new HashSet<(byte RowNumber, char Letter)>();
        if (seats.Any(seat => !seen.Add((seat.RowNumber, seat.Letter))))
        {
            throw new ArgumentException("Duplicate seat definitions found in the seat layout.");
        }
        _seats.AddRange(seats);
        AddDomainEvent(new AircraftCreatedEvent(Guid.NewGuid(), Id, TailNumber, EquipmentCode, CreatedAt));
    }
#pragma warning disable CS8618
    private Aircraft()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string TailNumber { get; private set; }
    public string EquipmentCode { get; private set; }
    public Weight DryOperatingWeight { get; private set; }
    public Weight MaximumTakeoffWeight { get; private set; }
    public Weight MaximumLandingWeight { get; private set; }
    public Weight MaximumZeroFuelWeight { get; private set; }
    public Weight MaximumFuelWeight { get; private set; }
    public AircraftSituation Situation { get; private set; }
    public TimeSpan InitialBlockTime { get; init; }
    public TimeSpan TotalBlockTime => InitialBlockTime + TimeSpan.FromTicks(_completedFlights.Sum(flight => flight.BlockTime.Ticks));
    public IReadOnlyList<CompletedFlight> CompletedFlights => _completedFlights.AsReadOnly();
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Aircraft Create(AircraftCreationArgs args) => new(args);
    public bool IsStaleFlightMovement(DateTimeOffset movementAt) =>
        movementAt <= Situation.EffectiveAt;

    public void MarkAsEnRoute(
        string destination,
        DateTimeOffset departedAt,
        DateTimeOffset updatedAt)
    {
        var next = AircraftSituation.EnRoute(
            destination,
            departedAt);

        ApplySituation(next, updatedAt);
    }

    public void MarkAsParked(
        string airport,
        DateTimeOffset arrivedAt,
        DateTimeOffset updatedAt)
    {
        var next = AircraftSituation.Parked(
            airport,
            arrivedAt);

        ApplySituation(next, updatedAt);
    }

    private void ApplySituation(
        AircraftSituation next,
        DateTimeOffset updatedAt)
    {
        if (!next.IsNewerThan(Situation))
        {
            throw new InvalidOperationException(
                "A flight movement can only be applied if it is " +
                "newer than the aircraft's current situation.");
        }

        Situation = next;
        UpdatedAt = updatedAt;
    }
}
