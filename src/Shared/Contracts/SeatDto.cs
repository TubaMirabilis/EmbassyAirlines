namespace Shared.Contracts;

public sealed record SeatDto(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string SeatNumber, string SeatType, bool IsAvailable, decimal Price);
