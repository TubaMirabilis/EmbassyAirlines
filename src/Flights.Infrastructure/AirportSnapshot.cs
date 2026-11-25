using NodaTime;

namespace Flights.Infrastructure;

public sealed record AirportSnapshot
{
    public required Guid Id { get; init; }
    public required long Version { get; init; }
    public required string Name { get; init; }
    public required string IcaoCode { get; init; }
    public required string IataCode { get; init; }
    public required string TimeZoneId { get; init; }
    public required Instant CreatedAt { get; init; }
    public required Instant UpdatedAt { get; init; }
}
