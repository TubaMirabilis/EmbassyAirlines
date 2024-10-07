namespace Shared.Contracts;

public sealed record FlightDto(Guid Id, DateTime CreatedAt, DateTime UpdatedAt, string FlightNumber, string Departure,
    string Destination, DateTime DepartureTime, DateTime ArrivalTime, decimal EconomyPrice, decimal BusinessPrice,
    int AvailableEconomySeats, int AvailableBusinessSeats);
