using AWS.Messaging.Telemetry.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });
        builder.Services
               .AddOpenTelemetry()
               .WithMetrics(metrics =>
               {
                   metrics.AddAspNetCoreInstrumentation();
                   metrics.AddHttpClientInstrumentation();
                   metrics.AddRuntimeInstrumentation();
                   metrics.AddAWSInstrumentation();
               }).WithTracing(tracing =>
               {
                   tracing.AddSource(builder.Environment.ApplicationName);
                   tracing.AddAspNetCoreInstrumentation(tracing => tracing.Filter = context =>
                   {
                       var path = context.Request.Path;
                       var stringComparison = StringComparison.OrdinalIgnoreCase;
                       return !path.StartsWithSegments(HealthEndpointPath, stringComparison) &&
                              !path.StartsWithSegments(AlivenessEndpointPath, stringComparison);
                   });
                   tracing.AddHttpClientInstrumentation();
                   tracing.AddAWSInstrumentation();
                   tracing.AddAWSMessagingInstrumentation();
                   tracing.AddAWSLambdaConfigurations();
               });
        builder.AddOpenTelemetryExporters();
        return builder;
    }
    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
        return builder;
    }
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
               .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }
        return app;
    }
}
