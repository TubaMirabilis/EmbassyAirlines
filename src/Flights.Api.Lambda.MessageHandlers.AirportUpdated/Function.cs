using System.Diagnostics;
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

namespace Flights.Api.Lambda.MessageHandlers.AirportUpdated;

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
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(AirportUpdatedTelemetry.SourceName));
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
        using var activity = AirportUpdatedTelemetry.Source.StartActivity("AirportUpdatedEventHandler.ProcessMessage");
        activity?.SetTag("messaging.message.id", message.MessageId);
        try
        {
            var messageElement = JsonElement.Parse(message.Body).GetProperty("Message").GetString();
            if (string.IsNullOrWhiteSpace(messageElement))
            {
                throw new JsonException("SNS message payload is null or empty.");
            }
            var airportUpdatedEvent = JsonElement.Parse(messageElement).GetProperty("data").Deserialize<AirportUpdatedEvent>();
            if (airportUpdatedEvent is null)
            {
                throw new JsonException("Failed to deserialize AirportUpdatedEvent.");
            }
            activity?.SetTag("airport.id", airportUpdatedEvent.AirportId);
            activity?.SetTag("airport.icao_code", airportUpdatedEvent.IcaoCode);
            activity?.SetTag("airport.iata_code", airportUpdatedEvent.IataCode);
            activity?.SetTag("airport.name", airportUpdatedEvent.Name);
            activity?.SetTag("airport.time_zone_id", airportUpdatedEvent.TimeZoneId);
            activity?.SetTag("event.id", airportUpdatedEvent.Id);
            activity?.SetTag("event.version", airportUpdatedEvent.Version);
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["EventId"] = airportUpdatedEvent.Id,
                ["AirportId"] = airportUpdatedEvent.AirportId,
                ["IcaoCode"] = airportUpdatedEvent.IcaoCode,
                ["IataCode"] = airportUpdatedEvent.IataCode,
                ["AirportName"] = airportUpdatedEvent.Name,
                ["TimeZoneId"] = airportUpdatedEvent.TimeZoneId,
                ["Version"] = airportUpdatedEvent.Version,
                ["SqsMessageId"] = message.MessageId
            });
            _logger.LogInformation("Processing airport update");
            using var serviceScope = _host.Services.CreateScope();
            var writer = serviceScope.ServiceProvider.GetRequiredService<IAirportProjectionWriter>();
            var snapshot = new AirportSnapshot
            {
                Id = airportUpdatedEvent.AirportId,
                Version = airportUpdatedEvent.Version,
                Name = airportUpdatedEvent.Name,
                IcaoCode = airportUpdatedEvent.IcaoCode,
                IataCode = airportUpdatedEvent.IataCode,
                TimeZoneId = airportUpdatedEvent.TimeZoneId,
                CreatedAt = Instant.FromDateTimeOffset(airportUpdatedEvent.CreatedAt),
                UpdatedAt = Instant.FromDateTimeOffset(airportUpdatedEvent.UpdatedAt)
            };
            await writer.ApplyAsync(snapshot, CancellationToken.None);
            _logger.LogInformation("Airport update processing completed");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
