namespace Flights.Api.Entities;

public sealed class Flight
{
    private Flight(string flightNumber, FlightSchedule schedule,
        FlightPricing pricing, AvailableSeats availableSeats, FlightStatus status)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        FlightNumber = flightNumber;
        Schedule = schedule;
        Pricing = pricing;
        AvailableSeats = availableSeats;
        Status = status;
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string FlightNumber { get; private set; }
    public FlightSchedule Schedule { get; private set; }
    public FlightPricing Pricing { get; private set; }
    public AvailableSeats AvailableSeats { get; private set; }
    public FlightStatus Status { get; private set; }
    public static Flight Create(string flightNumber, FlightSchedule schedule,
        FlightPricing pricing, AvailableSeats availableSeats, FlightStatus status)
        => new(flightNumber, schedule, pricing, availableSeats, status);
}

public sealed record FlightSchedule(string Departure,
    string Destination, DateTime DepartureTime, DateTime ArrivalTime);

public sealed record FlightPricing(decimal EconomyPrice, decimal BusinessPrice);

public sealed record AvailableSeats(int Economy, int Business);

public enum FlightStatus
{
    Scheduled,
    Delayed,
    CheckInClosed,
    Cancelled,
    Departed,
    Arrived
}
