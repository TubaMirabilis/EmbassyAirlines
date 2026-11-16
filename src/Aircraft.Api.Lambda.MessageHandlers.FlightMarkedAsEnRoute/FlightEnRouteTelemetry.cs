using System.Diagnostics;

namespace Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsEnRoute;

internal static class FlightEnRouteTelemetry
{
    public const string SourceName =
        "Aircraft.Api.Lambda.FlightMarkedAsEnRoute";

    public static readonly ActivitySource Source = new(SourceName);
}
