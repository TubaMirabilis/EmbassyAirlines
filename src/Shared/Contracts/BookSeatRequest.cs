namespace Shared.Contracts;

public sealed record BookSeatRequest(Guid SeatId, string PassengerName, string PassengerEmail);
