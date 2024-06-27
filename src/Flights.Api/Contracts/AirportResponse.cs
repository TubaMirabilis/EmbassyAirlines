namespace Flights.Api.Contracts;

public sealed record AirportResponse(Guid Id, string Iata, string Icao, string FullName, string City, string Country,
    string TimeZoneId, double Latitude, double Longitude);
