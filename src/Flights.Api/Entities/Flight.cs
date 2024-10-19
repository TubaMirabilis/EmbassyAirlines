using NodaTime;

namespace Flights.Api.Entities;

public sealed class Flight
{
    private readonly List<Seat> _seats = [];
    private Flight(string flightNumber, FlightSchedule schedule, FlightPricing pricing, IEnumerable<Seat> seats)
    {
        if (!seats.All(s => s.IsAvailable))
        {
            throw new ArgumentException("All seats must be available when creating a flight");
        }
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        FlightNumber = flightNumber;
        Schedule = schedule;
        Pricing = pricing;
        _seats.AddRange(seats);
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; private set; }
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }
    public string FlightNumber { get; private set; }
    public FlightSchedule Schedule { get; private set; }
    public FlightPricing Pricing { get; private set; }
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Flight Create(string flightNumber, FlightSchedule schedule,
        FlightPricing pricing, IEnumerable<Seat> seats)
        => new(flightNumber, schedule, pricing, seats);
}
public sealed record FlightSchedule
{
    public required Airport DepartureAirport { get; init; }
    public required Airport DestinationAirport { get; init; }
    public required ZonedDateTime DepartureTime { get; init; }
    public required ZonedDateTime ArrivalTime { get; init; }
}
public sealed record FlightPricing(decimal EconomyPrice, decimal BusinessPrice);
public sealed record Airport(string IataCode, string TimeZone);
