using NodaTime;

namespace Flights.Infrastructure;

public sealed record AircraftSnapshot
{
    public required Guid Id { get; init; }
    public required string TailNumber { get; init; }
    public required string EquipmentCode { get; init; }
    public required Instant CreatedAt { get; init; }
}
