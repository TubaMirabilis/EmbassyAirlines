using Flights.Api.Domain.Passengers;
using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Bookings;

public sealed class Booking
{
    private readonly List<Seat> _seats = [];
    private readonly List<Passenger> _passengers = [];
    private Booking(IEnumerable<Seat> seats, IEnumerable<Passenger> passengers, string reference, string? leadPassengerEmail)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        Reference = reference;
        LeadPassengerEmail = leadPassengerEmail ?? "";
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
    public string Reference { get; init; }
    public string LeadPassengerEmail { get; private set; }
    public IReadOnlyList<Passenger> Passengers => _passengers.AsReadOnly();
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Booking Create(IEnumerable<Seat> seats, IEnumerable<Passenger> passengers, string reference, string? leadPassengerEmail) => new(seats, passengers, reference, leadPassengerEmail);
}
