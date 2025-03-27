using NodaTime;

namespace Flights.Api.Lambda;

public sealed class Flight
{
    private Flight(FlightCreationArgs args)
    {
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
    public static Flight Create(FlightCreationArgs args) => new(args);
}
