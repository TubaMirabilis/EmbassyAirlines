namespace Flights.Api;

public sealed record CreateOrUpdateFlightDto(Guid AircraftId, string FlightNumberIata, string FlightNumberIcao, Guid DepartureAirportId, DateTime DepartureLocalTime, Guid ArrivalAirportId, DateTime ArrivalLocalTime, decimal EconomyPrice, decimal BusinessPrice);
