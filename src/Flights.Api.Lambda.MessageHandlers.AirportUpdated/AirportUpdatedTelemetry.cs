using System.Diagnostics;

namespace Flights.Api.Lambda.MessageHandlers.AirportUpdated;

internal static class AirportUpdatedTelemetry
{
    public const string SourceName =
        "Flights.Api.Lambda.AirportUpdated";

    public static readonly ActivitySource Source = new(SourceName);
}
