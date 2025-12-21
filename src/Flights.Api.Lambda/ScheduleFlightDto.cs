namespace Flights.Api.Lambda;

internal sealed record ScheduleFlightDto
{
    public required Guid AircraftId { get; init; }
    public required string FlightNumberIata { get; init; }
    public required string FlightNumberIcao { get; init; }
    public required Guid DepartureAirportId { get; init; }
    public required DateTime DepartureLocalTime { get; init; }
    public required Guid ArrivalAirportId { get; init; }
    public required DateTime ArrivalLocalTime { get; init; }
    public required decimal EconomyPrice { get; init; }
    public required decimal BusinessPrice { get; init; }
    public required string SchedulingAmbiguityPolicy { get; init; }
    public required string OperationType { get; init; }
}
