using NodaTime;

namespace Flights.Core.Models;

public sealed record AirportCreationArgs
{
    public required Instant CreatedAt { get; init; }
    public required string IataCode { get; init; }
    public required string IcaoCode { get; init; }
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string TimeZoneId { get; init; }
}
