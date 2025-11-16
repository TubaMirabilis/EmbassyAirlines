namespace Flights.Api;

internal sealed record FlightCreationArgs
{
    public required string FlightNumberIata { get; init; }
    public required string FlightNumberIcao { get; init; }
    public required FlightSchedule Schedule { get; init; }
    public required Aircraft Aircraft { get; init; }
    public required Money EconomyPrice { get; init; }
    public required Money BusinessPrice { get; init; }
}
