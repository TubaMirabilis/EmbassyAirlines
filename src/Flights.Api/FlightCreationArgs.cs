namespace Flights.Api;

internal sealed record FlightCreationArgs
{
    public required string FlightNumberIata { get; init; }
    public required string FlightNumberIcao { get; init; }
    public required DateTime DepartureLocalTime { get; init; }
    public required DateTime ArrivalLocalTime { get; init; }
    public required Airport DepartureAirport { get; init; }
    public required Airport ArrivalAirport { get; init; }
    public required Aircraft Aircraft { get; init; }
    public required Money EconomyPrice { get; init; }
    public required Money BusinessPrice { get; init; }
    public required SchedulingAmbiguityPolicy SchedulingAmbiguityPolicy { get; init; }
}
