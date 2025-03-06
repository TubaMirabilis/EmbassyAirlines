namespace Shared.Contracts;

public sealed record AirportUpdatedEvent(Guid Id, string Name, string IataCode, string TimeZoneId);
