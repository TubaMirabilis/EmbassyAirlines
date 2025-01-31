namespace Shared.Contracts;

public sealed record CreateAirportDto(string IataCode, string Name, string TimeZoneId);
