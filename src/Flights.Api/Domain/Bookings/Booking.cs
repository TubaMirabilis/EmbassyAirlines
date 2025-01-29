using Flights.Api.Domain.Passengers;
using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Bookings;

public sealed class Booking
{
    private readonly List<Seat> _seats = [];
    private readonly List<Passenger> _passengers = [];
    private Booking(IEnumerable<Seat> seats, IEnumerable<Passenger> passengers, Guid itineraryId)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        ItineraryId = itineraryId;
        _seats.AddRange(seats);
        _passengers.AddRange(passengers);
    }
#pragma warning disable CS8618
    private Booking()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public Instant UpdatedAt { get; private set; }
    public Guid ItineraryId { get; init; }
    public IReadOnlyList<Passenger> Passengers => _passengers.AsReadOnly();
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Booking Create(IEnumerable<Seat> seats, IEnumerable<Passenger> passengers, Guid itineraryId) => new(seats, passengers, itineraryId);
}
