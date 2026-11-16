using System.Diagnostics;

namespace Airports.Api.Lambda;

internal static class AirportsTelemetry
{
    public const string SourceName = "Airports.Api.Lambda";
    public static readonly ActivitySource Source = new(SourceName);
}
