namespace Shared.Contracts;

public sealed record AirportCreatedEvent(Guid Id, Guid AirportId, string Name, string IcaoCode, string IataCode, string TimeZoneId);
