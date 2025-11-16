using Flights.Api.Extensions;
using NodaTime;
using NodaTime.TimeZones;

namespace Flights.Api;

internal sealed record FlightSchedule
{
    public FlightSchedule(FlightScheduleCreationArgs args)
    {
        var resolver = ZoneLocalMappingResolver.FromSchedulingAmbiguityPolicy(args.SchedulingAmbiguityPolicy);
        DepartureTime = LocalDateTime.FromDateTime(args.DepartureLocalTime);
        var departureInstant = DepartureTime.InZone(args.DepartureAirport.TimeZone, resolver).ToInstant();
        if (departureInstant < SystemClock.Instance.GetCurrentInstant())
        {
            throw new InvalidOperationException("Departure time cannot be in the past");
        }
        ArrivalTime = LocalDateTime.FromDateTime(args.ArrivalLocalTime);
        var arrivalInstant = ArrivalTime.InZone(args.ArrivalAirport.TimeZone, resolver).ToInstant();
        if (arrivalInstant < departureInstant)
        {
            throw new InvalidOperationException("Arrival time cannot be before departure time");
        }
        ArrivalAirport = args.ArrivalAirport;
        DepartureAirport = args.DepartureAirport;
        SchedulingAmbiguityPolicy = args.SchedulingAmbiguityPolicy;
    }
#pragma warning disable CS8618
    private FlightSchedule()
    {
    }
#pragma warning restore CS8618
    public Airport ArrivalAirport { get; }
    public LocalDateTime ArrivalTime { get; }
    public Airport DepartureAirport { get; }
    public LocalDateTime DepartureTime { get; }
    public SchedulingAmbiguityPolicy SchedulingAmbiguityPolicy { get; }
}
