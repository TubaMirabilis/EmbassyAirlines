namespace Airports.Api.Lambda;

public sealed record CreateOrUpdateAirportDto(string IataCode, string Name, string TimeZoneId);
