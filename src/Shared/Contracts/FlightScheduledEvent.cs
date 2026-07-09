using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightScheduledEvent(Guid Id, Guid FlightId, string OperationType, decimal BusinessPrice, decimal EconomyPrice) : IDomainEvent;
