using System.Text.Json;
using Aircraft.Infrastructure;
using Aircraft.Infrastructure.Database;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Shared.Contracts;
using Shared.Messaging;
using Shared.Npgsql;
using Shared.ServiceDefaults;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsEnRoute;

public class Function
{
    private readonly IHost _host;
    private readonly ILogger<Function> _logger;
    private readonly TracerProvider _traceProvider;
    public Function()
    {
        var builder = new HostApplicationBuilder();
        var config = builder.Configuration;
        config.AddEnvironmentVariables(prefix: "AIRCRAFT_");
        builder.AddServiceDefaults();
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(FlightEnRouteTelemetry.SourceName));
        builder.Services.AddLogging();
        builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, false, "aircraft");
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<FlightDepartureNotificationProcessor>();
        _host = builder.Build();
        _logger = _host.Services.GetRequiredService<ILogger<Function>>();
        _traceProvider = _host.Services.GetRequiredService<TracerProvider>();
    }
    public Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
        => AWSLambdaWrapper.TraceAsync(_traceProvider, (e, ctx) => SqsBatchProcessor.ProcessAsync(e, ctx, ProcessMessageAsync), evnt, context);
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        using var activity = FlightEnRouteTelemetry.Source.StartActivity("FlightMarkedAsEnRouteEventHandler.ProcessMessage");
        var messageElement = JsonElement.Parse(message.Body).GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            throw new JsonException("SNS message payload is null or empty.");
        }
        var flightEvent = JsonElement.Parse(messageElement).GetProperty("data").Deserialize<FlightMarkedAsEnRouteEvent>();
        if (flightEvent is null)
        {
            throw new JsonException("Failed to deserialize FlightMarkedAsEnRouteEvent.");
        }
        activity?.SetTag("flight.id", flightEvent.FlightId);
        activity?.SetTag("flight.aircraft_id", flightEvent.AircraftId);
        activity?.SetTag("flight.arrival_airport_icao_code", flightEvent.ArrivalAirportIcaoCode);
        activity?.SetTag("event.id", flightEvent.Id);
        activity?.SetTag("messaging.message.id", message.MessageId);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventId"] = flightEvent.Id,
            ["FlightId"] = flightEvent.FlightId,
            ["AircraftId"] = flightEvent.AircraftId,
            ["ArrivalAirportIcaoCode"] = flightEvent.ArrivalAirportIcaoCode,
            ["SqsMessageId"] = message.MessageId
        });
        _logger.LogInformation("Processing en-route flight");
        using var serviceScope = _host.Services.CreateScope();
        var processor = serviceScope.ServiceProvider.GetRequiredService<FlightDepartureNotificationProcessor>();
        var result = await processor.ProcessAsync(flightEvent.Id, flightEvent.AircraftId, flightEvent.ArrivalAirportIcaoCode, flightEvent.OffBlockAt);
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
        {
            _logger.LogInformation("En-route flight processing completed with outcome {Outcome}", result);
        }
    }
}
