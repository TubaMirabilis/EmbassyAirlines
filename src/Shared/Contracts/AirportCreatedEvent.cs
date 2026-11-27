using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record AirportCreatedEvent : IDomainEvent
{
    public required Guid AirportId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string IataCode { get; init; }
    public required string IcaoCode { get; init; }
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string TimeZoneId { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required long Version { get; init; }
}
