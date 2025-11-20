namespace Flights.Api;

internal sealed record FlightScheduleCreationArgs
{
    public required Airport DepartureAirport { get; init; }
    public required DateTime DepartureLocalTime { get; init; }
    public required Airport ArrivalAirport { get; init; }
    public required DateTime ArrivalLocalTime { get; init; }
    public required SchedulingAmbiguityPolicy SchedulingAmbiguityPolicy { get; init; }
}
