namespace Shared.Contracts;

public sealed record AirportCreatedEvent(Guid Id, string Name, string IcaoCode, string IataCode, string TimeZoneId);
