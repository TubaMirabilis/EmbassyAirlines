using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightArrivedEvent(Guid Id, Guid AircraftId, Guid FlightId, string ArrivalAirportIcaoCode) : IFlightStatusManagementEvent;
