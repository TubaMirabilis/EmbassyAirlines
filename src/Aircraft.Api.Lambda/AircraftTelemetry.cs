using System.Diagnostics;

namespace Aircraft.Api.Lambda;

internal static class AircraftTelemetry
{
    public const string SourceName = "Aircraft.Api.Lambda";
    public static readonly ActivitySource Source = new(SourceName);
}
