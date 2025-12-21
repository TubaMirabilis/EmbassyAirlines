using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightMarkedAsEnRouteEvent(Guid FlightId, string ArrivalAirportIcaoCode) : IFlightStatusManagementEvent;
