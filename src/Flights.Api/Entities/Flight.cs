using NodaTime;

namespace Flights.Api.Entities;

public sealed class Flight
{
    private Flight(string flightNumber, FlightSchedule schedule, FlightPricing pricing, AvailableSeats availableSeats)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        FlightNumber = flightNumber;
        Schedule = schedule;
        Pricing = pricing;
        AvailableSeats = availableSeats;
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
    public AvailableSeats AvailableSeats { get; private set; }
    public static Flight Create(string flightNumber, FlightSchedule schedule, FlightPricing pricing, AvailableSeats availableSeats)
        => new(flightNumber, schedule, pricing, availableSeats);
}

public sealed record FlightSchedule
{
    public required Airport DepartureAirport { get; init; }
    public required Airport DestinationAirport { get; init; }
    public required ZonedDateTime DepartureTime { get; init; }
    public required ZonedDateTime ArrivalTime { get; init; }
}

public sealed record FlightPricing(decimal EconomyPrice, decimal BusinessPrice);

public sealed record AvailableSeats(int Economy, int Business);

public sealed record Airport(string IataCode, string TimeZone);
