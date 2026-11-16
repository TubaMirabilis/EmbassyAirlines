using System.Diagnostics;

namespace Flights.Api.Lambda.MessageHandlers.AirportCreated;

internal static class AirportCreatedTelemetry
{
    public const string SourceName =
        "Flights.Api.Lambda.AirportCreated";

    public static readonly ActivitySource Source = new(SourceName);
}
