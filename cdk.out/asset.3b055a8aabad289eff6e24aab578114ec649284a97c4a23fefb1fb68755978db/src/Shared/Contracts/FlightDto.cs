namespace Shared.Contracts;

public sealed record FlightDto(Guid Id, string FlightNumberIata, string FlightNumberIcao, Guid DepartureAirportId,
    string DepartureAirportIataCode, string DepartureAirportIcaoCode, string DepartureAirportName,
    string DepartureAirportTimeZoneId, Guid ArrivalAirportId, string ArrivalAirportIataCode,
    string ArrivalAirportIcaoCode, string ArrivalAirportName, string ArrivalAirportTimeZoneId,
    DateTimeOffset DepartureTime, DateTimeOffset ArrivalTime, TimeSpan Duration, decimal EconomyPrice,
    decimal BusinessPrice, Guid AircraftId, string AircraftEquipmentCode, string AircraftTailNumber, string SchedulingAmbiguityPolicy);
