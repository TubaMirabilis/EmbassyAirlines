using System.Diagnostics;

namespace Flights.Api.Lambda.MessageHandlers.AircraftCreated;

internal static class AircraftCreatedTelemetry
{
    public const string SourceName =
        "Flights.Api.Lambda.AircraftCreated";

    public static readonly ActivitySource Source = new(SourceName);
}
