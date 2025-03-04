namespace Shared.Contracts;

public sealed record FlightDto(Guid Id, string FlightNumber, string Departure, string Destination,
    string DepartureTime, string ArrivalTime, TimeSpan Duration, decimal CheapestEconomyPrice,
    decimal CheapestBusinessPrice, int AvailableEconomySeats, int AvailableBusinessSeats);
