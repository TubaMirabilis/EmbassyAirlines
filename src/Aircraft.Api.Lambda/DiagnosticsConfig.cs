using System.Diagnostics;

namespace Aircraft.Api.Lambda;

internal static class DiagnosticsConfig
{
    public static readonly ActivitySource ActivitySource = new("Aircraft.Api.Lambda");
}
