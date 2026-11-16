using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Shared.Contracts;
using Shared.Messaging;
using Shared.Npgsql;
using Shared.ServiceDefaults;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Flights.Api.Lambda.MessageHandlers.AirportCreated;

public class Function
{
    private readonly IHost _host;
    private readonly ILogger<Function> _logger;
    private readonly TracerProvider _traceProvider;
    public Function()
    {
        var builder = new HostApplicationBuilder();
        var config = builder.Configuration;
        config.AddEnvironmentVariables(prefix: "FLIGHTS_");
        builder.AddServiceDefaults();
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(AirportCreatedTelemetry.SourceName));
        builder.Services.AddLogging();
        builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, true, "flights");
        builder.Services.AddScoped<IAirportProjectionWriter, AirportProjectionWriter>();
        _host = builder.Build();
        _logger = _host.Services.GetRequiredService<ILogger<Function>>();
        _traceProvider = _host.Services.GetRequiredService<TracerProvider>();
    }
    public Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
        => AWSLambdaWrapper.TraceAsync(_traceProvider, (e, ctx) => SqsBatchProcessor.ProcessAsync(e, ctx, ProcessMessageAsync), evnt, context);
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        using var activity = AirportCreatedTelemetry.Source.StartActivity("AirportCreatedEventHandler.ProcessMessage");
        var messageElement = JsonElement.Parse(message.Body).GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            throw new JsonException("SNS message payload is null or empty.");
        }
        var airportCreatedEvent = JsonElement.Parse(messageElement).GetProperty("data").Deserialize<AirportCreatedEvent>();
        if (airportCreatedEvent is null)
        {
            throw new JsonException("Failed to deserialize AirportCreatedEvent.");
        }
        activity?.SetTag("airport.id", airportCreatedEvent.AirportId);
        activity?.SetTag("airport.icao_code", airportCreatedEvent.IcaoCode);
        activity?.SetTag("airport.iata_code", airportCreatedEvent.IataCode);
        activity?.SetTag("airport.name", airportCreatedEvent.Name);
        activity?.SetTag("airport.time_zone_id", airportCreatedEvent.TimeZoneId);
        activity?.SetTag("event.id", airportCreatedEvent.Id);
        activity?.SetTag("event.version", airportCreatedEvent.Version);
        activity?.SetTag("messaging.message.id", message.MessageId);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventId"] = airportCreatedEvent.Id,
            ["AirportId"] = airportCreatedEvent.AirportId,
            ["IcaoCode"] = airportCreatedEvent.IcaoCode,
            ["IataCode"] = airportCreatedEvent.IataCode,
            ["AirportName"] = airportCreatedEvent.Name,
            ["TimeZoneId"] = airportCreatedEvent.TimeZoneId,
            ["Version"] = airportCreatedEvent.Version,
            ["SqsMessageId"] = message.MessageId
        });
        _logger.LogInformation("Processing airport creation");
        using var serviceScope = _host.Services.CreateScope();
        var writer = serviceScope.ServiceProvider.GetRequiredService<IAirportProjectionWriter>();
        var snapshot = new AirportSnapshot(airportCreatedEvent.AirportId, airportCreatedEvent.Version, airportCreatedEvent.Name, airportCreatedEvent.IcaoCode, airportCreatedEvent.IataCode, airportCreatedEvent.TimeZoneId, Instant.FromDateTimeOffset(airportCreatedEvent.CreatedAt), Instant.FromDateTimeOffset(airportCreatedEvent.UpdatedAt));
        await writer.ApplyAsync(snapshot, CancellationToken.None);
        _logger.LogInformation("Airport creation processing completed");
    }
}
