using Flights.Api.Domain.Airports;
using Flights.Api.Domain.Seats;
using NodaTime;

namespace Flights.Api.Domain.Flights;

public sealed record FlightCreationArgs
{
    public required Airport ArrivalAirport { get; init; }
    public required LocalDateTime ArrivalLocalTime { get; init; }
    public required Airport DepartureAirport { get; init; }
    public required LocalDateTime DepartureLocalTime { get; init; }
    public required string FlightNumber { get; init; }
    public required IEnumerable<Seat> Seats { get; init; }
}
