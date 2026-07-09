using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightRescheduledEvent(Guid Id, Guid FlightId, DateTime DepartureLocalTime, DateTime ArrivalLocalTime) : IDomainEvent;
