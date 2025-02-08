using Flights.Api.Domain.Flights;
using Flights.Api.Domain.Passengers;
using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Bookings;

public sealed class Booking
{
    private readonly List<Passenger> _passengers = [];
    private Booking(IEnumerable<Passenger> passengers, Flight flight)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        IsCancelled = false;
        Flight = flight;
        FlightId = flight.Id;
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
    public bool IsCancelled { get; private set; }
    public decimal TotalPrice => GetSeats().Sum(s => s.Price);
    public Flight Flight { get; init; }
    public Guid FlightId { get; init; }
    public IReadOnlyList<Passenger> Passengers => _passengers.AsReadOnly();
    public void Cancel()
    {
        if (IsCancelled)
        {
            throw new InvalidOperationException("Booking is already cancelled");
        }
        Flight.Seats.Where(s => _passengers.Any(p => p.Id == s.PassengerId))
                    .ToList()
                    .ForEach(s => s.CancelBooking());
        IsCancelled = true;
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }
    public void RemovePassenger(Guid passengerId)
    {
        if (!_passengers.Any(p => p.Id == passengerId))
        {
            throw new InvalidOperationException("Passenger not found in booking");
        }
        var seats = Flight.Seats.Where(s => s.PassengerId == passengerId).ToList();
        seats.ForEach(s => s.CancelBooking());
        _passengers.RemoveAll(p => p.Id == passengerId);
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }
    public IEnumerable<Seat> GetSeats()
    {
        var passengerIds = _passengers.Select(p => (Guid?)p.Id).ToList();
        return Flight.Seats
                     .Where(s => passengerIds.Contains(s.PassengerId));
    }
    public static Booking Create(IEnumerable<Passenger> passengers, Flight flight) => new(passengers, flight);
}
