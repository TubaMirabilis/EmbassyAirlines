using System.Diagnostics;

namespace Flights.Publisher.Lambda;

internal static class PublisherTelemetry
{
    public const string SourceName = "Flights.Publisher.Lambda";
    public static readonly ActivitySource Source = new(SourceName);
}
