namespace Shared.Contracts;

public sealed record SeatDto(Guid Id, string SeatNumber, string SeatType, bool IsBooked, decimal Price);
