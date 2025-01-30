namespace Shared.Contracts;

public sealed record BookingDto(Guid Id, string FlightNumber, Dictionary<Guid, KeyValuePair<PassengerDto, SeatDto>> Passengers);
