namespace Shared.Contracts;

public sealed record FlightScheduledEvent(Guid FlightId, string OperationType, decimal BusinessPrice, decimal EconomyPrice);
