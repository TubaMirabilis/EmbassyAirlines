using Flights.Api.Extensions;
using NodaTime;
using NodaTime.TimeZones;

namespace Flights.Api;

internal sealed class Flight
{
    private Flight(FlightCreationArgs args)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        FlightNumberIata = args.FlightNumberIata;
        FlightNumberIcao = args.FlightNumberIcao;
        DepartureLocalTime = args.Schedule.DepartureTime;
        ArrivalLocalTime = args.Schedule.ArrivalTime;
        DepartureAirport = args.Schedule.DepartureAirport;
        ArrivalAirport = args.Schedule.ArrivalAirport;
        Aircraft = args.Aircraft;
        EconomyPrice = args.EconomyPrice;
        BusinessPrice = args.BusinessPrice;
        SchedulingAmbiguityPolicy = args.Schedule.SchedulingAmbiguityPolicy;
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public string FlightNumberIata { get; private set; }
    public string FlightNumberIcao { get; private set; }
    public LocalDateTime DepartureLocalTime { get; private set; }
    public LocalDateTime ArrivalLocalTime { get; private set; }
    public SchedulingAmbiguityPolicy SchedulingAmbiguityPolicy { get; private set; }
    public ZonedDateTime DepartureZonedTime
    {
        get
        {
            var resolver = ZoneLocalMappingResolver.FromSchedulingAmbiguityPolicy(SchedulingAmbiguityPolicy);
            return DepartureLocalTime.InZone(DepartureAirport.TimeZone, resolver);
        }
    }
    public ZonedDateTime ArrivalZonedTime
    {
        get
        {
            var resolver = ZoneLocalMappingResolver.FromSchedulingAmbiguityPolicy(SchedulingAmbiguityPolicy);
            return ArrivalLocalTime.InZone(ArrivalAirport.TimeZone, resolver);
        }
    }
    public Airport DepartureAirport { get; init; }
    public Airport ArrivalAirport { get; init; }
    public Aircraft Aircraft { get; private set; }
    public Money EconomyPrice { get; private set; }
    public Money BusinessPrice { get; private set; }
    public Instant DepartureInstant => DepartureZonedTime.ToInstant();
    public Instant ArrivalInstant => ArrivalZonedTime.ToInstant();
    public static Flight Create(FlightCreationArgs args) => new(args);
    public void AssignAircraft(Aircraft aircraft) => Aircraft = aircraft;
    public void AdjustPricing(Money economyPrice, Money businessPrice)
    {
        EconomyPrice = economyPrice;
        BusinessPrice = businessPrice;
    }
    public void Reschedule(DateTime departureLocalTime, DateTime arrivalLocalTime, SchedulingAmbiguityPolicy policy)
    {
        var schedule = new FlightSchedule(new FlightScheduleCreationArgs
        {
            DepartureAirport = DepartureAirport,
            DepartureLocalTime = departureLocalTime,
            ArrivalAirport = ArrivalAirport,
            ArrivalLocalTime = arrivalLocalTime,
            SchedulingAmbiguityPolicy = policy
        });
        DepartureLocalTime = schedule.DepartureTime;
        ArrivalLocalTime = schedule.ArrivalTime;
        SchedulingAmbiguityPolicy = schedule.SchedulingAmbiguityPolicy;
    }
}
