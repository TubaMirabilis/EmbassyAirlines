using AWS.Messaging.Telemetry.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.ServiceDefaults;

internal static class OpenTelemetryExtensions
{
    internal static TBuilder ConfigureOpenTelemetry<TBuilder>(
        this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService(
                    builder.Environment.ApplicationName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddAWSInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(
                    builder.Environment.ApplicationName);
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddAWSInstrumentation();
                tracing.AddAWSMessagingInstrumentation();
                tracing.AddAWSLambdaConfigurations();
            });
        var useOtlpExporter = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }
}
