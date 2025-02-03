namespace Shared.Contracts;

public sealed record FlightDto(Guid Id, string FlightNumber, string Departure, string Destination, DateTimeOffset DepartureTime, DateTimeOffset ArrivalTime, TimeSpan Duration, decimal CheapestEconomyPrice, decimal CheapestBusinessPrice, int AvailableEconomySeats, int AvailableBusinessSeats);
