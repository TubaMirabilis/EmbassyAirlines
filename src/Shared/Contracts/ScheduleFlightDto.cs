namespace Shared.Contracts;

public sealed record ScheduleFlightDto(string FlightNumber, Guid DepartureAirportId, DateTime DepartureLocalTime,
    Guid ArrivalAirportId, DateTime ArrivalLocalTime, decimal EconomyPrice, decimal BusinessPrice, string EquipmentType);
