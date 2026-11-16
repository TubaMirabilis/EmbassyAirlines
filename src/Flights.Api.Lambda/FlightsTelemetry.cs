using System.Diagnostics;

namespace Flights.Api.Lambda;

internal static class FlightsTelemetry
{
    public const string SourceName = "Flights.Api.Lambda";
    public static readonly ActivitySource Source = new(SourceName);
}
