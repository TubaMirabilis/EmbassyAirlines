namespace Shared.Contracts;

public sealed record SeatDto(string FlightNumber, string SeatNumber, string SeatType, bool IsBooked, decimal Price);
