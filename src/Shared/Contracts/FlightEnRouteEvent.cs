using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightEnRouteEvent(Guid FlightId) : IFlightStatusManagementEvent;
