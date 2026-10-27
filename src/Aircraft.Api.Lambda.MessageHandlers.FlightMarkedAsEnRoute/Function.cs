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
        using var activity = Activity.Current?.Source.StartActivity("FlightMarkedAsEnRouteEventHandler.ProcessMessage");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            throw new JsonException("SNS message payload is null or empty.");
        }
        var flightMarkedAsEnRouteEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<FlightMarkedAsEnRouteEvent>();
        if (flightMarkedAsEnRouteEvent is null)
        {
            throw new JsonException("Failed to deserialize FlightMarkedAsEnRouteEvent.");
        }
        activity?.SetTag("flight.id", flightMarkedAsEnRouteEvent.FlightId);
        activity?.SetTag("flight.aircraft_id", flightMarkedAsEnRouteEvent.AircraftId);
        activity?.SetTag("flight.arrival_airport_icao_code", flightMarkedAsEnRouteEvent.ArrivalAirportIcaoCode);
        context.Logger.LogInformation($"Processing flightMarkedAsEnRouteEvent with ID: {flightMarkedAsEnRouteEvent.Id}");
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<AircraftFlightStatusProcessor>();
        var result = await processor.ProcessAsync(flightMarkedAsEnRouteEvent.Id, flightMarkedAsEnRouteEvent.AircraftId, Core.Models.Status.EnRoute, flightMarkedAsEnRouteEvent.ArrivalAirportIcaoCode, flightMarkedAsEnRouteEvent.OccurredAt);
        context.Logger.LogInformation("flightMarkedAsEnRouteEvent {EventId} for aircraft {AircraftId} completed with outcome {Outcome}", flightMarkedAsEnRouteEvent.Id, flightMarkedAsEnRouteEvent.AircraftId, result);
    }
}
