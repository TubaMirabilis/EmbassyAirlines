using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightCancelledEvent(Guid Id, Guid FlightId) : IFlightStatusManagementEvent;
