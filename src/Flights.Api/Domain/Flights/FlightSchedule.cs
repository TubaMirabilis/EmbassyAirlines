using NodaTime;

namespace Flights.Api.Domain.Flights;

public sealed record FlightSchedule
{
    public required Airport DepartureAirport { get; init; }
    public required Airport DestinationAirport { get; init; }
    public required ZonedDateTime DepartureTime { get; init; }
    public required ZonedDateTime ArrivalTime { get; init; }
}
