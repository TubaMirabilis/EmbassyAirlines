namespace Deployment;

internal static class Observability
{
    // Returns a copy of the supplied Lambda environment with the standard
    // OpenTelemetry resource variables added. Trace export itself is driven by
    // enabling AWS X-Ray active tracing on each function (Tracing.ACTIVE); these
    // variables simply give the OTel SDK a stable service identity in AWS. The
    // application only activates its OTLP exporter when OTEL_EXPORTER_OTLP_ENDPOINT
    // is present, so that key is intentionally left unset here — set it (to a
    // reachable collector) to also ship spans over OTLP.
    internal static Dictionary<string, string> WithOtel(this IReadOnlyDictionary<string, string> environment, string serviceName) =>
        new(environment)
        {
            ["OTEL_SERVICE_NAME"] = serviceName,
            ["OTEL_RESOURCE_ATTRIBUTES"] = "service.namespace=EmbassyAirlines"
        };
}
