using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightDelayedEnRouteEvent(Guid FlightId) : IFlightStatusManagementEvent;
