namespace Flights.Api;

internal sealed record CreateOrUpdateFlightDto(Guid AircraftId, string FlightNumberIata, string FlightNumberIcao, Guid DepartureAirportId, DateTime DepartureLocalTime, Guid ArrivalAirportId, DateTime ArrivalLocalTime, decimal EconomyPrice, decimal BusinessPrice, string SchedulingAmbiguityPolicy);
