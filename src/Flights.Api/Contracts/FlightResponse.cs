namespace Flights.Api.Contracts;

public sealed record FlightResponse(Guid Id, DateTime CreatedAt, DateTime UpdatedAt, string Number,
    string NumberIataFormat, string NumberIcaoFormat, DateTime DepartureTimeUtc, DateTime ArrivalTimeUtc,
    AircraftResponse Aircraft, string Status, string DepartureGate, string ArrivalGate, string DepartureTerminal,
    string ArrivalTerminal, AirportResponse DepartureAirport, AirportResponse ArrivalAirport, short Distance,
    short AdultMen, short AdultWomen, short Children, short CheckedBags, string Notes, string? DepartureTaf,
    string? ArrivalTaf, string? DepartureMetar, string? ArrivalMetar, DateTime? ActualDepartureTimeUtc,
    DateTime? ActualArrivalTimeUtc, short TotalPassengers, string Duration, DateTime DepartureTimeLocal,
    DateTime ArrivalTimeLocal);
