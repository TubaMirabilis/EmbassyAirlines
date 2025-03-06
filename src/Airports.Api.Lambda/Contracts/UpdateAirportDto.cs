namespace Airports.Api.Lambda.Contracts;

public sealed record UpdateAirportDto(string IataCode, string Name, string TimeZoneId);
