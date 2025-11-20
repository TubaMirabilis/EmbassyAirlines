namespace Airports.Api.Lambda;

internal sealed record CreateOrUpdateAirportDto(string IcaoCode, string IataCode, string Name, string TimeZoneId);
