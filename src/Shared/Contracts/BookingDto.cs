namespace Shared.Contracts;

public sealed record BookingDto(Guid Id, IEnumerable<SeatDto> Seats, IEnumerable<PassengerDto> Passengers);
