namespace Shared.Contracts;

public sealed record BookingDto(string FlightNumber, string DepartureTime, string Departure, string Destination,
    string DepartureIata, string DestinationIata, Dictionary<Guid, KeyValuePair<PassengerDto, SeatDto>> Passengers);
