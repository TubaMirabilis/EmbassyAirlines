using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightMarkedAsDelayedEnRouteEvent(Guid FlightId, string ArrivalAirportIcaoCode) : IFlightStatusManagementEvent;
