using System.Diagnostics;

namespace Aircraft.Publisher.Lambda;

internal static class PublisherTelemetry
{
    public const string SourceName = "Aircraft.Publisher.Lambda";
    public static readonly ActivitySource Source = new(SourceName);
}
