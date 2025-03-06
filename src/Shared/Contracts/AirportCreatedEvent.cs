namespace Shared.Contracts;

public sealed record AirportCreatedEvent(Guid Id, string Name, string IataCode, string TimeZoneId);
