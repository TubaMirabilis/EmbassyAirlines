namespace Shared.Contracts;

public sealed record FlightDto(string FlightNumber, string Departure, string Destination,
    DateTimeOffset DepartureTime, DateTimeOffset ArrivalTime, decimal CheapestEconomyPrice,
    decimal CheapestBusinessPrice, int AvailableEconomySeats, int AvailableBusinessSeats);
