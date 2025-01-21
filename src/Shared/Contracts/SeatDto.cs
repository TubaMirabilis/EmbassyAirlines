namespace Shared.Contracts;

public sealed record SeatDto(string SeatNumber, string SeatType, bool IsAvailable, decimal Price);
