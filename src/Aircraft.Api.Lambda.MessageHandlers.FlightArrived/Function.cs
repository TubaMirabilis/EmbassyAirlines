using System.Diagnostics;
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

namespace Aircraft.Api.Lambda.MessageHandlers.FlightArrived;

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
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(FlightArrivalTelemetry.SourceName));
        builder.Services.AddLogging();
        builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, false, "aircraft");
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<FlightArrivalNotificationProcessor>();
        _host = builder.Build();
        _logger = _host.Services.GetRequiredService<ILogger<Function>>();
        _traceProvider = _host.Services.GetRequiredService<TracerProvider>();
    }
    public Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
        => AWSLambdaWrapper.TraceAsync(_traceProvider, (e, ctx) => SqsBatchProcessor.ProcessAsync(e, ctx, ProcessMessageAsync), evnt, context);
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        using var activity = FlightArrivalTelemetry.Source.StartActivity("FlightArrivedEventHandler.ProcessMessage");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            throw new JsonException("SNS message payload is null or empty.");
        }
        var flightArrivedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<FlightArrivedEvent>();
        if (flightArrivedEvent is null)
        {
            throw new JsonException("Failed to deserialize FlightArrivedEvent.");
        }
        activity?.SetTag("flight.id", flightArrivedEvent.FlightId);
        activity?.SetTag("flight.aircraft_id", flightArrivedEvent.AircraftId);
        activity?.SetTag("flight.arrival_airport_icao_code", flightArrivedEvent.ArrivalAirportIcaoCode);
        activity?.SetTag("flight.departure_airport_icao_code", flightArrivedEvent.DepartureAirportIcaoCode);
        activity?.SetTag("event.id", flightArrivedEvent.Id);
        activity?.SetTag("messaging.message.id", message.MessageId);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventId"] = flightArrivedEvent.Id,
            ["FlightId"] = flightArrivedEvent.FlightId,
            ["AircraftId"] = flightArrivedEvent.AircraftId,
            ["ArrivalAirportIcaoCode"] = flightArrivedEvent.ArrivalAirportIcaoCode,
            ["DepartureAirportIcaoCode"] = flightArrivedEvent.DepartureAirportIcaoCode,
            ["SqsMessageId"] = message.MessageId
        });
        _logger.LogInformation("Processing flight arrival");
        using var serviceScope = _host.Services.CreateScope();
        var processor = serviceScope.ServiceProvider.GetRequiredService<FlightArrivalNotificationProcessor>();
        var result = await processor.ProcessAsync(flightArrivedEvent);
        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
        {
            _logger.LogInformation("Flight arrival processing completed with outcome {Outcome}", result);
        }
    }
}
