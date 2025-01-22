namespace Shared.Contracts;

public sealed record SeatDto(string SeatNumber, string SeatType, bool IsBooked, decimal Price);
