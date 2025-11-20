namespace Shared.Contracts;

public sealed record AirportDto(Guid Id, string Name, string IcaoCode, string IataCode, string TimeZoneId);
