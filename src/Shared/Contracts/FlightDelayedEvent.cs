using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightDelayedEvent(Guid FlightId) : IFlightStatusManagementEvent;
