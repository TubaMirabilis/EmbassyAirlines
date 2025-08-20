using NodaTime;

namespace Flights.Api;

public sealed record FlightCreationArgs
{
    public required string FlightNumber { get; init; }
    public required LocalDateTime DepartureLocalTime { get; init; }
    public required LocalDateTime ArrivalLocalTime { get; init; }
    public required Airport DepartureAirport { get; init; }
    public required Airport ArrivalAirport { get; init; }
    public required Aircraft Aircraft { get; init; }
    public required Money EconomyPrice { get; init; }
    public required Money BusinessPrice { get; init; }
}
