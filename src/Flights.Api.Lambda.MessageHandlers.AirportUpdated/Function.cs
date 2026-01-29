using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

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
        builder.Services.AddDatabaseConnection(config);
        builder.Services.AddSingleton<IClock>(SystemClock.Instance);
        _host = builder.Build();
        _traceProvider = _host.Services.GetRequiredService<TracerProvider>();
    }
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context) => await AWSLambdaWrapper.TraceAsync(_traceProvider, async (e, ctx) =>
    {
        foreach (var message in e.Records)
        {
            await ProcessMessageAsync(message, ctx);
        }
    }, evnt, context);
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        using var activity = Activity.Current?.Source.StartActivity("AirportUpdatedEventHandler.ProcessMessage");
        context.Logger.LogInformation($"Processed message {message.Body}");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            context.Logger.LogWarning("Message element is null or empty.");
            return;
        }
        var airportUpdatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AirportUpdatedEvent>();
        if (airportUpdatedEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize AirportUpdatedEvent.");
            return;
        }
        activity?.SetTag("airport.id", airportUpdatedEvent.AirportId);
        activity?.SetTag("airport.icao_code", airportUpdatedEvent.IcaoCode);
        activity?.SetTag("airport.iata_code", airportUpdatedEvent.IataCode);
        activity?.SetTag("airport.name", airportUpdatedEvent.Name);
        activity?.SetTag("airport.time_zone_id", airportUpdatedEvent.TimeZoneId);
        context.Logger.LogInformation($"Processing AirportUpdatedEvent with ID: {airportUpdatedEvent.Id}");
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var airport = await dbContext.Airports.FindAsync(airportUpdatedEvent.AirportId);
        if (airport is null)
        {
            context.Logger.LogWarning($"Airport with ID {airportUpdatedEvent.AirportId} not found. Skipping update.");
            return;
        }
        var clock = _host.Services.GetRequiredService<IClock>();
        var now = clock.GetCurrentInstant();
        airport.Update(airportUpdatedEvent.IcaoCode, airportUpdatedEvent.IataCode, airportUpdatedEvent.Name, airportUpdatedEvent.TimeZoneId, now);
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Airport with ID {airportUpdatedEvent.AirportId} updated successfully.");
    }
}
