namespace Shared.Contracts;

public sealed record AirportUpdatedEvent(Guid Id, Guid AirportId, string Name, string IcaoCode, string IataCode, string TimeZoneId);
