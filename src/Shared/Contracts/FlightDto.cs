namespace Shared.Contracts;

public sealed record FlightDto(Guid Id, string FlightNumber, Guid DepartureAirportId, string DepartureAirportIataCode,
    string DepartureAirportTimeZoneId, Guid ArrivalAirportId, string ArrivalAirportIataCode,
    string ArrivalAirportTimeZoneId, string DepartureLocalTime, string ArrivalLocalTime, TimeSpan Duration,
    decimal EconomyPrice, decimal BusinessPrice, Guid AircraftId, string AircraftEquipmentCode, string AircraftTailNumber);
