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

namespace Flights.Api.Lambda.MessageHandlers.AircraftCreated;


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
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource(AircraftCreatedTelemetry.SourceName));
        builder.Services.AddLogging();
        builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, true, "flights");
        builder.Services.AddScoped<IAircraftProjectionWriter, AircraftProjectionWriter>();
        _host = builder.Build();
        _logger = _host.Services.GetRequiredService<ILogger<Function>>();
        _traceProvider = _host.Services.GetRequiredService<TracerProvider>();
    }
    public Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
        => AWSLambdaWrapper.TraceAsync(_traceProvider, (e, ctx) => SqsBatchProcessor.ProcessAsync(e, ctx, ProcessMessageAsync), evnt, context);
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        using var activity = AircraftCreatedTelemetry.Source.StartActivity("AircraftCreatedEventHandler.ProcessMessage");
        activity?.SetTag("messaging.message.id", message.MessageId);
        try
        {
            var messageElement = JsonElement.Parse(message.Body).GetProperty("Message").GetString();
            if (string.IsNullOrWhiteSpace(messageElement))
            {
                throw new JsonException("SNS message payload is null or empty.");
            }
            var aircraftCreatedEvent = JsonElement.Parse(messageElement).GetProperty("data").Deserialize<AircraftCreatedEvent>();
            if (aircraftCreatedEvent is null)
            {
                throw new JsonException("Failed to deserialize AircraftCreatedEvent.");
            }
            activity?.SetTag("aircraft.id", aircraftCreatedEvent.AircraftId);
            activity?.SetTag("aircraft.tail_number", aircraftCreatedEvent.TailNumber);
            activity?.SetTag("aircraft.equipment_code", aircraftCreatedEvent.EquipmentCode);
            activity?.SetTag("event.id", aircraftCreatedEvent.Id);
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["EventId"] = aircraftCreatedEvent.Id,
                ["AircraftId"] = aircraftCreatedEvent.AircraftId,
                ["TailNumber"] = aircraftCreatedEvent.TailNumber,
                ["EquipmentCode"] = aircraftCreatedEvent.EquipmentCode,
                ["SqsMessageId"] = message.MessageId
            });
            _logger.LogInformation("Processing aircraft creation");
            using var serviceScope = _host.Services.CreateScope();
            var writer = serviceScope.ServiceProvider.GetRequiredService<IAircraftProjectionWriter>();
            await writer.InsertIfAbsentAsync(aircraftCreatedEvent.AircraftId, aircraftCreatedEvent.TailNumber, aircraftCreatedEvent.EquipmentCode, Instant.FromDateTimeOffset(aircraftCreatedEvent.CreatedAt), CancellationToken.None);
            _logger.LogInformation("Aircraft creation processing completed");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
