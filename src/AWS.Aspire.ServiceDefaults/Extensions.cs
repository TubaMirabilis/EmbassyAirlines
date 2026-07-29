using System.Reflection;
using AWS.Messaging.Telemetry.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Shared;
using Shared.Extensions;
using Shared.Middleware;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
        return builder;
    }
    public static TBuilder AddHttpApiLambdaDefaults<TBuilder>(this TBuilder builder, Assembly assembly) where TBuilder : IHostApplicationBuilder
    {
        builder.AddServiceDefaults();
        var services = builder.Services;
        services.AddEndpoints(assembly);
        services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi();
        return builder;
    }
    public static WebApplication UseDefaultPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<RequestContextLoggingMiddleware>();
        app.UseSerilogRequestLogging();
        return app;
    }
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        app.MapEndpoints();
        return app;
    }
    private static void ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });
        builder.Services
               .AddOpenTelemetry()
               .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
               .WithMetrics(metrics =>
               {
                   metrics.AddAspNetCoreInstrumentation();
                   metrics.AddHttpClientInstrumentation();
                   metrics.AddRuntimeInstrumentation();
                   metrics.AddAWSInstrumentation();
               }).WithTracing(tracing =>
               {
                   tracing.AddSource(builder.Environment.ApplicationName);
                   tracing.AddAspNetCoreInstrumentation();
                   tracing.AddHttpClientInstrumentation();
                   tracing.AddAWSInstrumentation();
                   tracing.AddAWSMessagingInstrumentation();
                   tracing.AddAWSLambdaConfigurations();
               });
    }
}
