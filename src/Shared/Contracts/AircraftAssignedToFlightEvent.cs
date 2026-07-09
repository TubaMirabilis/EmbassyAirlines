using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record AircraftAssignedToFlightEvent(Guid Id, Guid FlightId, Guid AircraftId) : IDomainEvent;
