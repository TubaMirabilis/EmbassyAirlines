namespace Shared.Contracts;

public sealed record BookingDto(Guid Id, string Reference, IEnumerable<SeatDto> Seats, IEnumerable<PassengerDto> Passengers);
