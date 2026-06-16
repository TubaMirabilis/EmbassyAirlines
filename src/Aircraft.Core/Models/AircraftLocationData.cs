namespace Aircraft.Core.Models;

public sealed record AircraftLocationData
{
    public AircraftLocationData(Status status, string? parkedAt, string? enRouteTo)
    {
        Validate(status, parkedAt, enRouteTo);
        Status = status;
        ParkedAt = Normalize(parkedAt);
        EnRouteTo = Normalize(enRouteTo);
    }
    private static void Validate(Status status, string? parkedAt, string? enRouteTo)
    {
        var hasParkedAt = !string.IsNullOrWhiteSpace(parkedAt);
        var hasEnRouteTo = !string.IsNullOrWhiteSpace(enRouteTo);
        var error = status switch
        {
            Status.Parked when !hasParkedAt => "Status is Parked, so ParkedAt must be provided.",
            Status.Parked when hasEnRouteTo => "Status is Parked, so EnRouteTo must be empty.",
            Status.EnRoute when !hasEnRouteTo => "Status is EnRoute, so EnRouteTo must be provided.",
            Status.EnRoute when hasParkedAt => "Status is EnRoute, so ParkedAt must be empty.",
            _ => null
        };
        if (error is not null)
        {
            throw new ArgumentException(error);
        }
    }
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    private AircraftLocationData()
    {
    }
    public Status Status { get; }
    public string? ParkedAt { get; }
    public string? EnRouteTo { get; }
}
