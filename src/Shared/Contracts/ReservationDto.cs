namespace Shared.Contracts;

public sealed record ReservationDto(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    string ReservationNumber, string Departure, string Destination, DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime, string SeatNumber, string SeatType, decimal Price, string PassengerName,
    string PassengerEmail, string PassengerPhoneNumber);
