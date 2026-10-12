using NodaTime;

namespace Flights.Infrastructure;

public sealed record AirportSnapshot(
    Guid Id,
    long Version,
    string Name,
    string IcaoCode,
    string IataCode,
    string TimeZoneId,
    Instant CreatedAt,
    Instant UpdatedAt);
