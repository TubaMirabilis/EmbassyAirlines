using NodaTime;

namespace Flights.Api.Lambda;

public sealed record FlightCreationArgs
{
    public required Airport ArrivalAirport { get; init; }
    public required LocalDateTime ArrivalLocalTime { get; init; }
    public required Airport DepartureAirport { get; init; }
    public required LocalDateTime DepartureLocalTime { get; init; }
    public required string FlightNumber { get; init; }
}
