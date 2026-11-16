using System.Diagnostics;

namespace Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute;

internal static class FlightDelayedEnRouteTelemetry
{
    public const string SourceName =
        "Aircraft.Api.Lambda.FlightMarkedAsDelayedEnRoute";

    public static readonly ActivitySource Source = new(SourceName);
}
