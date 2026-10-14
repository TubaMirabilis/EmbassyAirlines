using System.Diagnostics;
using System.Text.Json;
using Aircraft.Infrastructure;
using Aircraft.Infrastructure.Database;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using AWS.Aspire.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Shared.Contracts;
using Shared.Npgsql;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute;

public class Function
{
    private readonly IHost _host;
    private readonly TracerProvider _traceProvider;
    public Function()
    {
        var builder = new HostApplicationBuilder();
        var config = builder.Configuration;
        config.AddEnvironmentVariables(prefix: "AIRCRAFT_");
        builder.AddServiceDefaults();
        builder.Services.AddLogging();
        builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, false, "aircraft");
        builder.Services.AddScoped<AircraftFlightStatusProcessor>();
        _host = builder.Build();
        _traceProvider = _host.Services.GetRequiredService<TracerProvider>();
    }
    public Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context) => AWSLambdaWrapper.TraceAsync(_traceProvider, (e, ctx) => SqsBatchProcessor.ProcessAsync(e, ctx, ProcessMessageAsync), evnt, context);
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        using var activity = Activity.Current?.Source.StartActivity("FlightMarkedAsDelayedEnRouteEventHandler.ProcessMessage");
        context.Logger.LogInformation($"Processed message {message.Body}");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            throw new JsonException("SNS message payload is null or empty.");
        }
        var flightMarkedAsDelayedEnRouteEvent = JsonDocument.Parse(messageElement)
                                                            .RootElement
                                                            .GetProperty("data")
                                                            .Deserialize<FlightMarkedAsDelayedEnRouteEvent>();
        if (flightMarkedAsDelayedEnRouteEvent is null)
        {
            throw new JsonException("Failed to deserialize FlightMarkedAsDelayedEnRouteEvent.");
        }
        activity?.SetTag("flight.id", flightMarkedAsDelayedEnRouteEvent.FlightId);
        activity?.SetTag("flight.aircraft_id", flightMarkedAsDelayedEnRouteEvent.AircraftId);
        activity?.SetTag("flight.arrival_airport_icao_code", flightMarkedAsDelayedEnRouteEvent.ArrivalAirportIcaoCode);
        context.Logger.LogInformation($"Processing flightMarkedAsDelayedEnRouteEvent with ID: {flightMarkedAsDelayedEnRouteEvent.Id}");
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<AircraftFlightStatusProcessor>();
        var result = await processor.ProcessAsync(flightMarkedAsDelayedEnRouteEvent.AircraftId, Core.Models.Status.EnRoute, flightMarkedAsDelayedEnRouteEvent.ArrivalAirportIcaoCode, flightMarkedAsDelayedEnRouteEvent.OccurredAt);
        var finalMessage = result switch
        {
            AircraftFlightStatusProcessingResult.Applied => $"Aircraft with ID {flightMarkedAsDelayedEnRouteEvent.AircraftId} marked as EnRoute to {flightMarkedAsDelayedEnRouteEvent.ArrivalAirportIcaoCode}.",
            AircraftFlightStatusProcessingResult.Stale => $"Stale event for aircraft with ID {flightMarkedAsDelayedEnRouteEvent.AircraftId}. Event occurred at {flightMarkedAsDelayedEnRouteEvent.OccurredAt}, but the aircraft was last updated at an earlier time.",
            _ => throw new ArgumentOutOfRangeException($"Unexpected result from AircraftFlightStatusProcessor: {result}.")
        };
        context.Logger.LogInformation(finalMessage);
    }
}
