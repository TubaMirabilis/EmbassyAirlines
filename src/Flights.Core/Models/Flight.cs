using NodaTime;
using NodaTime.TimeZones;
using Shared;

namespace Flights.Core.Models;

public sealed class Flight
{
    private Flight(FlightCreationArgs args)
    {
        Ensure.NotNullOrEmpty(args.FlightNumberIata);
        Ensure.NotNullOrEmpty(args.FlightNumberIcao);
        Id = Guid.NewGuid();
        CreatedAt = args.CreatedAt;
        UpdatedAt = CreatedAt;
        FlightNumberIata = args.FlightNumberIata;
        FlightNumberIcao = args.FlightNumberIcao;
        Status = FlightStatus.Scheduled;
        DepartureLocalTime = args.Schedule.DepartureTime;
        ArrivalLocalTime = args.Schedule.ArrivalTime;
        DepartureAirport = args.Schedule.DepartureAirport;
        ArrivalAirport = args.Schedule.ArrivalAirport;
        Aircraft = args.Aircraft;
        EconomyPrice = args.EconomyPrice;
        BusinessPrice = args.BusinessPrice;
        SchedulingAmbiguityPolicy = args.Schedule.SchedulingAmbiguityPolicy;
        OperationType = args.OperationType;
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public Instant UpdatedAt { get; private set; }
    public string FlightNumberIata { get; private set; }
    public string FlightNumberIcao { get; private set; }
    public FlightStatus Status { get; private set; }
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
    public Aircraft Aircraft { get; private set; }
    public Airport ArrivalAirport { get; init; }
    public Instant ArrivalInstant => ArrivalZonedTime.ToInstant();
    public ZonedDateTime ArrivalZonedTime
    {
        get
        {
            var resolver = ZoneLocalMappingResolver.FromSchedulingAmbiguityPolicy(SchedulingAmbiguityPolicy);
            return ArrivalLocalTime.InZone(ArrivalAirport.TimeZone, resolver);
        }
    }
    public Money BusinessPrice { get; private set; }
    public Airport DepartureAirport { get; init; }
    public Instant DepartureInstant => DepartureZonedTime.ToInstant();
    public Money EconomyPrice { get; private set; }
    public OperationType OperationType { get; init; }
    public static Flight Create(FlightCreationArgs args) => new(args);
    public void AssignAircraft(Aircraft aircraft, Instant instant)
    {
        Aircraft = aircraft;
        UpdatedAt = instant;
    }
    public void AdjustStatus(FlightStatus status, Instant instant)
    {
        if (!FlightStatusTransitions.CanTransition(Status, status))
        {
            throw new ArgumentException($"Cannot transition flight status from {Status} to {status}");
        }
        Status = status;
        UpdatedAt = instant;
    }
    public void AdjustPricing(Money economyPrice, Money businessPrice, Instant instant)
    {
        BusinessPrice = businessPrice;
        EconomyPrice = economyPrice;
        UpdatedAt = instant;
    }
    public void Reschedule(FlightSchedule schedule, Instant instant)
    {
        ArrivalLocalTime = schedule.ArrivalTime;
        DepartureLocalTime = schedule.DepartureTime;
        SchedulingAmbiguityPolicy = schedule.SchedulingAmbiguityPolicy;
        UpdatedAt = instant;
    }
}
