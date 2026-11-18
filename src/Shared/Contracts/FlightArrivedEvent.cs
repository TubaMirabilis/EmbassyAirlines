using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightArrivedEvent : IFlightStatusManagementEvent
{
    public required Guid Id { get; init; }
    public required Guid AircraftId { get; init; }
    public required Guid FlightId { get; init; }
    public required string DepartureAirportIcaoCode { get; init; }
    public required string ArrivalAirportIcaoCode { get; init; }
    public required DateTimeOffset OffBlockAt { get; init; }
    public required DateTimeOffset TakeOffAt { get; init; }
    public required DateTimeOffset LandingAt { get; init; }
    public required DateTimeOffset OnBlockAt { get; init; }
}
