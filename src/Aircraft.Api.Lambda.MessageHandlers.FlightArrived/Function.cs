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

namespace Aircraft.Api.Lambda.MessageHandlers.FlightArrived;

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
        using var activity = Activity.Current?.Source.StartActivity("FlightArrivedEventHandler.ProcessMessage");
        context.Logger.LogInformation($"Processed message {message.Body}");
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
        context.Logger.LogInformation(
            "Processing arrival of flight {FlightId} operated by aircraft {AircraftId} " +
            "at airport {ArrivalAirportIcaoCode}. Event ID: {EventId}",
            flightArrivedEvent.FlightId,
            flightArrivedEvent.AircraftId,
            flightArrivedEvent.ArrivalAirportIcaoCode,
            flightArrivedEvent.Id);
        using var scope = _host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<AircraftFlightStatusProcessor>();
        await processor.ProcessAsync(flightArrivedEvent.AircraftId, Core.Models.Status.Parked, flightArrivedEvent.ArrivalAirportIcaoCode, flightArrivedEvent.OccurredAt);
        context.Logger.LogInformation($"Aircraft with ID {flightArrivedEvent.AircraftId} marked as parked at {flightArrivedEvent.ArrivalAirportIcaoCode}.");
    }
}
