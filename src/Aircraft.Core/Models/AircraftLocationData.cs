namespace Aircraft.Core.Models;

public sealed record AircraftLocationData
{
    public AircraftLocationData(Status status, string? parkedAt, string? enRouteTo)
    {
        var hasParkedAt = !string.IsNullOrWhiteSpace(parkedAt);
        var hasEnRouteTo = !string.IsNullOrWhiteSpace(enRouteTo);
        if (status == Status.Parked && !hasParkedAt)
        {
            throw new ArgumentException("Status is Parked, so ParkedAt must be provided.");
        }
        if (status == Status.Parked && hasEnRouteTo)
        {
            throw new ArgumentException("Status is Parked, so EnRouteTo must be empty.");
        }
        if (status == Status.EnRoute && !hasEnRouteTo)
        {
            throw new ArgumentException("Status is EnRoute, so EnRouteTo must be provided.");
        }
        if (status == Status.EnRoute && hasParkedAt)
        {
            throw new ArgumentException("Status is EnRoute, so ParkedAt must be empty.");
        }
        Status = status;
        ParkedAt = parkedAt?.Trim().ToUpperInvariant();
        EnRouteTo = enRouteTo?.Trim().ToUpperInvariant();
    }
    private AircraftLocationData()
    {
    }
    public Status Status { get; }
    public string? ParkedAt { get; }
    public string? EnRouteTo { get; }
}
