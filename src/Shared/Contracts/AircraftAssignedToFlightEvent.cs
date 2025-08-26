namespace Shared.Contracts;

public sealed record AircraftAssignedToFlightEvent(Guid FlightId, Guid AircraftId);
