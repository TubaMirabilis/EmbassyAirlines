namespace Shared.Contracts;

public sealed record AirportDto(Guid Id, string Name, string IataCode, string TimeZoneId);
