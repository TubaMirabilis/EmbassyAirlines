namespace Shared.Contracts;

public sealed record FlightScheduledEvent(Guid FlightId, decimal BusinessPrice, decimal EconomyPrice);
