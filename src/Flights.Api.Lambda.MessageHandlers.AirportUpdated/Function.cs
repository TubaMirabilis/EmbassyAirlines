using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using AWS.Aspire.ServiceDefaults;
using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Shared.Contracts;
using Shared.Npgsql;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Flights.Api.Lambda.MessageHandlers.AirportUpdated;

public class Function
{
    private readonly IHost _host;
    private readonly TracerProvider _traceProvider;
    public Function()
    {
        var builder = new HostApplicationBuilder();
        var config = builder.Configuration;
        config.AddEnvironmentVariables(prefix: "FLIGHTS_");
        builder.AddServiceDefaults();
        builder.Services.AddLogging();
        builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, true, "flights");
        builder.Services.AddScoped<IAirportProjectionWriter, AirportProjectionWriter>();
        _host = builder.Build();
        _traceProvider = _host.Services.GetRequiredService<TracerProvider>();
    }
    public Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context) => AWSLambdaWrapper.TraceAsync(_traceProvider, (e, ctx) => SqsBatchProcessor.ProcessAsync(e, ctx, ProcessMessageAsync), evnt, context);
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        using var activity = Activity.Current?.Source.StartActivity("AirportUpdatedEventHandler.ProcessMessage");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            throw new JsonException("SNS message payload is null or empty.");
        }
        var airportUpdatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AirportUpdatedEvent>();
        if (airportUpdatedEvent is null)
        {
            throw new JsonException("Failed to deserialize AirportUpdatedEvent.");
        }
        activity?.SetTag("airport.id", airportUpdatedEvent.AirportId);
        activity?.SetTag("airport.icao_code", airportUpdatedEvent.IcaoCode);
        activity?.SetTag("airport.iata_code", airportUpdatedEvent.IataCode);
        activity?.SetTag("airport.name", airportUpdatedEvent.Name);
        activity?.SetTag("airport.time_zone_id", airportUpdatedEvent.TimeZoneId);
        context.Logger.LogInformation($"Processing AirportUpdatedEvent with ID: {airportUpdatedEvent.Id}");
        using var scope = _host.Services.CreateScope();
        var writer = scope.ServiceProvider
    .GetRequiredService<IAirportProjectionWriter>();
        var snapshot = new AirportSnapshot(
            airportUpdatedEvent.AirportId,
            airportUpdatedEvent.Version,
            airportUpdatedEvent.Name,
            airportUpdatedEvent.IcaoCode,
            airportUpdatedEvent.IataCode,
            airportUpdatedEvent.TimeZoneId,
            Instant.FromDateTimeOffset(airportUpdatedEvent.CreatedAt),
            Instant.FromDateTimeOffset(airportUpdatedEvent.UpdatedAt));
        await writer.ApplyAsync(snapshot, CancellationToken.None);
        context.Logger.LogInformation($"Successfully processed AirportUpdatedEvent with ID: {airportUpdatedEvent.Id}");
    }
}
