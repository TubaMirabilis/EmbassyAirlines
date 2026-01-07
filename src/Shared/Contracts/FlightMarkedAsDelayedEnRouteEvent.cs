using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightMarkedAsDelayedEnRouteEvent(Guid Id, Guid AircraftId, Guid FlightId, string ArrivalAirportIcaoCode) : IFlightStatusManagementEvent;
