using System.Diagnostics;

namespace Flights.Api.Lambda;

internal static class DiagnosticsConfig
{
    public static readonly ActivitySource ActivitySource = new("Flights.Api.Lambda");
}
