namespace Flights.Api.Contracts;

public sealed record AddOrUpdateFlightRequest(string Number, string NumberIataFormat, string NumberIcaoFormat,
    DateTime DepartureTimeUtc, DateTime ArrivalTimeUtc, Guid AircraftId, string Status, string DepartureGate,
    string ArrivalGate, string DepartureTerminal, string ArrivalTerminal, Guid DepartureAirportId,
    Guid ArrivalAirportId, short AdultMen, short AdultWomen, short Children, short CheckedBags, string Notes,
    string? DepartureTaf, string? ArrivalTaf, string? DepartureMetar, string? ArrivalMetar,
    DateTime? ActualDepartureTimeUtc, DateTime? ActualArrivalTimeUtc);
