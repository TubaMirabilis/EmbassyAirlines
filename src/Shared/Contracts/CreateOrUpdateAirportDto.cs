namespace Shared.Contracts;

public sealed record CreateOrUpdateAirportDto(string IcaoCode, string IataCode, string Name, string TimeZoneId);
