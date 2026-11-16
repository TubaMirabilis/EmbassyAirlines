using System.Diagnostics;

namespace Airports.Publisher.Lambda;

internal static class PublisherTelemetry
{
    public const string SourceName = "Airports.Publisher.Lambda";
    public static readonly ActivitySource Source = new(SourceName);
}
