using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Flights;

public sealed class Flight
{
    private readonly List<Seat> _seats = [];
    private Flight(string flightNumber, FlightSchedule schedule, IEnumerable<Seat> seats)
    {
        var seatsList = seats.ToList();
        if (seatsList.Any(s => s.IsBooked))
        {
            throw new ArgumentException("All seats must be available when creating a flight");
        }
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        FlightNumber = flightNumber;
        Schedule = schedule;
        _seats.AddRange(seatsList);
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
    public decimal CheapestEconomyPrice => Seats.Where(s => s.SeatType == SeatType.Economy)
                                                .Min(s => s.Price);
    public decimal CheapestBusinessPrice => Seats.Where(s => s.SeatType == SeatType.Business)
                                                 .Min(s => s.Price);
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public static Flight Create(string flightNumber, FlightSchedule schedule, IEnumerable<Seat> seats)
        => new(flightNumber, schedule, seats);
}
