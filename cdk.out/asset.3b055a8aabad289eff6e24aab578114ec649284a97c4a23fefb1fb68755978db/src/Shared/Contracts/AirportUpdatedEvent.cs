namespace Shared.Contracts;

public sealed record AirportUpdatedEvent(Guid Id, string Name, string IcaoCode, string IataCode, string TimeZoneId);
