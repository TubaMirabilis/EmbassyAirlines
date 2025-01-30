using Flights.Api.Domain.Flights;
using Flights.Api.Domain.Passengers;
using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Bookings;

public sealed class Booking
{
    private readonly List<Passenger> _passengers = [];
    private Booking(IEnumerable<Passenger> passengers, Flight flight, Guid itineraryId)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        Flight = flight;
        FlightId = flight.Id;
        ItineraryId = itineraryId;
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
    public Flight Flight { get; init; }
    public Guid FlightId { get; init; }
    public Guid ItineraryId { get; init; }
    public IReadOnlyList<Passenger> Passengers => _passengers.AsReadOnly();
    public IReadOnlyList<Seat> Seats => Flight.Seats
                                              .Where(s => s.BookingId == Id)
                                              .ToList()
                                              .AsReadOnly();
    public static Booking Create(IEnumerable<Passenger> passengers, Flight flight, Guid itineraryId) => new(passengers, flight, itineraryId);
}
