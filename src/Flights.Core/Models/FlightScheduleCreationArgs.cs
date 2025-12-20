using NodaTime;

namespace Flights.Core.Models;

public sealed record FlightScheduleCreationArgs
{
    public required Airport ArrivalAirport { get; init; }
    public required LocalDateTime ArrivalLocalTime { get; init; }
    public required Airport DepartureAirport { get; init; }
    public required LocalDateTime DepartureLocalTime { get; init; }
    public required Instant Now { get; init; }
    public required SchedulingAmbiguityPolicy SchedulingAmbiguityPolicy { get; init; }
}
