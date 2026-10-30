namespace Aircraft.Infrastructure;

// CompletedFlight is null only for a Duplicate event, whose history was never evaluated.
public sealed record FlightArrivalProcessingResult(
    CompletedFlightRecordingResult? CompletedFlight,
    AircraftFlightStatusProcessingResult AircraftStatus)
{
    public static FlightArrivalProcessingResult Duplicate { get; } =
        new(null, AircraftFlightStatusProcessingResult.Duplicate);
}
