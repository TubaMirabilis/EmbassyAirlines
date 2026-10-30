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
        Status = args.AircraftLocationData.Status;
        ParkedAt = args.AircraftLocationData.ParkedAt;
        EnRouteTo = args.AircraftLocationData.EnRouteTo;
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
    /// <summary>
    /// When the flight movement (an off-block or on-block) that set the aircraft's current
    /// location took place, or <see langword="null"/> if it has not moved since it was created.
    /// Used only to order flight movement events; <see cref="UpdatedAt"/> records modifications.
    /// </summary>
    public DateTimeOffset? LastFlightMovementAt { get; private set; }
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
    public IReadOnlyList<CompletedFlight> CompletedFlights => _completedFlights.AsReadOnly();
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Aircraft Create(AircraftCreationArgs args) => new(args);
    /// <summary>
    /// Whether a flight movement that took place at <paramref name="movementAt"/> is no newer than
    /// the aircraft's current location, which is known as of its last flight movement or, if it has
    /// not moved, as of its creation. <see cref="UpdatedAt"/> plays no part, so modifying the aircraft
    /// for any other reason cannot cause a later-reported movement to be discarded.
    /// </summary>
    public bool IsStaleFlightMovement(DateTimeOffset movementAt) =>
        movementAt <= (LastFlightMovementAt ?? CreatedAt);
    public void MarkAsEnRoute(string destination, DateTimeOffset departedAt, DateTimeOffset updatedAt)
    {
        Ensure.NotNullOrEmpty(destination);
        EnsureNotStale(departedAt);
        EnRouteTo = destination.Trim().ToUpperInvariant();
        ParkedAt = null;
        Status = Status.EnRoute;
        LastFlightMovementAt = departedAt;
        UpdatedAt = updatedAt;
    }
    public void MarkAsParked(string location, DateTimeOffset arrivedAt, DateTimeOffset updatedAt)
    {
        Ensure.NotNullOrEmpty(location);
        EnsureNotStale(arrivedAt);
        ParkedAt = location.Trim().ToUpperInvariant();
        EnRouteTo = null;
        Status = Status.Parked;
        LastFlightMovementAt = arrivedAt;
        UpdatedAt = updatedAt;
    }
    private void EnsureNotStale(DateTimeOffset movementAt)
    {
        if (IsStaleFlightMovement(movementAt))
        {
            throw new InvalidOperationException(
                "A flight movement can only be applied if it is newer than the aircraft's current location.");
        }
    }
}
