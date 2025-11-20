namespace Shared.Contracts;

public sealed record FlightPricingAdjustedEvent(Guid FlightId, decimal EconomyPrice, decimal BusinessPrice);
