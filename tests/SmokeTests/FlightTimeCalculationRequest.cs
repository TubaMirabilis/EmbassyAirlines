using NodaTime;

namespace SmokeTests;

internal sealed record FlightTimeCalculationRequest
{
    public required Duration LeadTime { get; init; }
    public required Duration FlightDuration { get; init; }
    public required string DepartureTimeZoneId { get; init; }
    public required string ArrivalTimeZoneId { get; init; }
}
