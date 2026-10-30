using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightArrivedEvent(Guid Id, Guid AircraftId, Guid FlightId, string DepartureAirportIcaoCode, string ArrivalAirportIcaoCode, DateTimeOffset OffBlockAt, DateTimeOffset TakeOffAt, DateTimeOffset LandingAt, DateTimeOffset OnBlockAt) : IFlightStatusManagementEvent;
