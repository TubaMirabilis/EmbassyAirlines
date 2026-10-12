using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record AirportUpdatedEvent(Guid Id, Guid AirportId, long Version, string Name, string IcaoCode, string IataCode, string TimeZoneId, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt) : IDomainEvent;
