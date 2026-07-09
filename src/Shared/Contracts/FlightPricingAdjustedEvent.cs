using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightPricingAdjustedEvent(Guid Id, Guid FlightId, decimal EconomyPrice, decimal BusinessPrice) : IDomainEvent;
