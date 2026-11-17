namespace Aircraft.Core.Models;

public sealed record CompletedFlightCreationArgs
{
    public required string ArrivalAirportIcao { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string DepartureAirportIcao { get; init; }
    public required Guid Id { get; init; }
    public required FlightMilestones Milestones { get; init; }
}
