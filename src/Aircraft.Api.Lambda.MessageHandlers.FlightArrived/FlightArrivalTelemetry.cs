using System.Diagnostics;

namespace Aircraft.Api.Lambda.MessageHandlers.FlightArrived;

internal static class FlightArrivalTelemetry
{
    public const string SourceName = "Aircraft.Api.Lambda.FlightArrived";
    public static readonly ActivitySource Source = new(SourceName);
}
