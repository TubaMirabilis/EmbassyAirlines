using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightDelayedEvent(Guid Id, Guid FlightId) : IFlightStatusManagementEvent;
