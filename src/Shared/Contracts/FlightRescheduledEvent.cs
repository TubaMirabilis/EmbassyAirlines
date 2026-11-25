using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightRescheduledEvent : IDomainEvent
{
    public required DateTime ArrivalLocalTime { get; init; }
    public required DateTime DepartureLocalTime { get; init; }
    public required Guid FlightId { get; init; }
    public required Guid Id { get; init; }
}
