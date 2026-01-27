using System.Diagnostics;

namespace Airports.Api.Lambda;

internal static class DiagnosticsConfig
{
    public static readonly ActivitySource ActivitySource = new("Airports.Api.Lambda");
}
