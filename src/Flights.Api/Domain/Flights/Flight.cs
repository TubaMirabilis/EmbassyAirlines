using Flights.Api.Domain.Airports;
using Flights.Api.Domain.Passengers;
using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Flights;

public sealed class Flight
{
    private readonly List<Seat> _seats = [];
    private Flight(FlightCreationArgs args)
    {
        var seatsList = args.Seats.ToList();
        if (seatsList.Any(s => s.IsBooked))
        {
            throw new ArgumentException("All seats must be available when creating a flight");
        }
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        FlightNumber = args.FlightNumber;
        DepartureAirportId = args.DepartureAirport.Id;
        DepartureAirport = args.DepartureAirport;
        DepartureLocalTime = args.DepartureLocalTime;
        ArrivalAirportId = args.ArrivalAirport.Id;
        ArrivalAirport = args.ArrivalAirport;
        ArrivalLocalTime = args.ArrivalLocalTime;
        _seats.AddRange(seatsList);
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; private set; }
    public Instant UpdatedAt { get; private set; }
    public string FlightNumber { get; private set; }
    public LocalDateTime DepartureLocalTime { get; set; }
    public LocalDateTime ArrivalLocalTime { get; set; }
    public Guid DepartureAirportId { get; set; }
    public Airport DepartureAirport { get; set; }
    public Guid ArrivalAirportId { get; set; }
    public Airport ArrivalAirport { get; set; }
    public ZonedDateTime ScheduledDeparture => DepartureLocalTime.InZoneLeniently(DepartureAirport.TimeZone);
    public ZonedDateTime ScheduledArrival => ArrivalLocalTime.InZoneLeniently(ArrivalAirport.TimeZone);
    public Instant DepartureInstant => ScheduledDeparture.ToInstant();
    public Instant ArrivalInstant => ScheduledArrival.ToInstant();
    public decimal CheapestEconomyPrice => Seats.Where(s => s.SeatType == SeatType.Economy)
                                                .Min(s => s.Price);
    public decimal CheapestBusinessPrice => Seats.Where(s => s.SeatType == SeatType.Business)
                                                 .Min(s => s.Price);
    public IReadOnlyList<Seat> Seats => _seats.AsReadOnly();
    public void BookSeats(Dictionary<Guid, Passenger> passengers)
    {
        foreach (var (seatId, passenger) in passengers)
        {
            var seat = _seats.Single(s => s.Id == seatId);
            seat.Book(passenger.Id);
        }
    }
    public static Flight Create(FlightCreationArgs args) => new(args);
}
