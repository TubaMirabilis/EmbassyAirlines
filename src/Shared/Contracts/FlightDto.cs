namespace Shared.Contracts;

public sealed record FlightDto(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string FlightNumber, string Departure, string Destination, DateTimeOffset DepartureTime, DateTimeOffset ArrivalTime, decimal EconomyPrice, decimal BusinessPrice, int AvailableEconomySeats, int AvailableBusinessSeats);
