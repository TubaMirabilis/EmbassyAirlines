using Shared.Abstractions;

namespace Shared.Contracts;

public sealed record FlightArrivedEvent(Guid FlightId, string ArrivalAirportIcaoCode) : IFlightStatusManagementEvent;
