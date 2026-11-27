using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightMarkedAsDelayedEnRouteEvent : IFlightStatusManagementEvent
{
    public required Guid AircraftId { get; init; }
    public required string ArrivalAirportIcaoCode { get; init; }
    public required Guid FlightId { get; init; }
    public required Guid Id { get; init; }
    public required DateTimeOffset OffBlockAt { get; init; }
}
