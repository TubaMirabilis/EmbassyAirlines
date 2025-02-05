namespace Shared.Contracts;

public sealed record BookingDto(string FlightNumber, Dictionary<Guid, KeyValuePair<PassengerDto, SeatDto>> Passengers);
