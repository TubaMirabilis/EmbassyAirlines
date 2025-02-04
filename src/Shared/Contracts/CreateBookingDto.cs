namespace Shared.Contracts;

public sealed record CreateBookingDto(Dictionary<Guid, PassengerDto> Seats, Guid FlightId);
