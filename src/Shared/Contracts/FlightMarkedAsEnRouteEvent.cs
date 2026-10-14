using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightMarkedAsEnRouteEvent(Guid Id, Guid AircraftId, Guid FlightId, string ArrivalAirportIcaoCode, DateTimeOffset OccurredAt) : IFlightStatusManagementEvent;
