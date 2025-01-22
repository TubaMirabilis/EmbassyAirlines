namespace Shared.Contracts;

public sealed record BookingDto(Guid Id, string Reference, Guid FlightId, string SeatNumber, string PassengerName, string? PassengerEmail);
