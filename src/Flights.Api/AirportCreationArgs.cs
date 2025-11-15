namespace Flights.Api;

internal sealed record AirportCreationArgs
{
    public required string IataCode { get; init; }
    public required string IcaoCode { get; init; }
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string TimeZoneId { get; init; }
}
