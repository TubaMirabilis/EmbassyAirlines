namespace Shared.Contracts;

public sealed record BookingDto(Guid Id, string Reference, string FlightNumber, string SeatNumber, string SeatType, decimal Price, string PassengerName, string? PassengerEmail);
