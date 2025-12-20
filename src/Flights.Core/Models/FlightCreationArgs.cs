using NodaTime;

namespace Flights.Core.Models;

public sealed record FlightCreationArgs
{
    public required Aircraft Aircraft { get; init; }
    public required Money BusinessPrice { get; init; }
    public required Instant CreatedAt { get; init; }
    public required Money EconomyPrice { get; init; }
    public required string FlightNumberIata { get; init; }
    public required string FlightNumberIcao { get; init; }
    public required FlightSchedule Schedule { get; init; }
}
