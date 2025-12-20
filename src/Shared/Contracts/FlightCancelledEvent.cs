using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightCancelledEvent(Guid FlightId) : IFlightStatusManagementEvent;
