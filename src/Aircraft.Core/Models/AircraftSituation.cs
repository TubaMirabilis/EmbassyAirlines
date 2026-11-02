namespace Aircraft.Core.Models;

public sealed record AircraftSituation
{
    private AircraftSituation(
        Status status,
        string? parkedAt,
        string? enRouteTo,
        DateTimeOffset effectiveAt)
    {
        Status = status;
        ParkedAt = parkedAt;
        EnRouteTo = enRouteTo;
        EffectiveAt = effectiveAt;
    }
    private AircraftSituation()
    {
    }
    public Status Status { get; }
    public string? ParkedAt { get; }
    public string? EnRouteTo { get; }
    public DateTimeOffset EffectiveAt { get; }

    public static AircraftSituation Parked(
        string airport,
        DateTimeOffset effectiveAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(airport);

        return new AircraftSituation(
            Status.Parked,
            airport.Trim().ToUpperInvariant(),
            null,
            effectiveAt);
    }

    public static AircraftSituation EnRoute(
        string destination,
        DateTimeOffset effectiveAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        return new AircraftSituation(
            Status.EnRoute,
            null,
            destination.Trim().ToUpperInvariant(),
            effectiveAt);
    }

    public bool IsNewerThan(AircraftSituation other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return EffectiveAt > other.EffectiveAt;
    }
}
