using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Flights.Core.Models;
using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Flights.Api.Lambda.MessageHandlers.AirportCreated;

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
        using var activity = Activity.Current?.Source.StartActivity("AirportCreatedEventHandler.ProcessMessage");
        context.Logger.LogInformation($"Processed message {message.Body}");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            context.Logger.LogWarning("Message element is null or empty.");
            return;
        }
        var airportCreatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AirportCreatedEvent>();
        if (airportCreatedEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize AirportCreatedEvent.");
            return;
        }
        activity?.SetTag("airport.id", airportCreatedEvent.AirportId);
        activity?.SetTag("airport.icao_code", airportCreatedEvent.IcaoCode);
        activity?.SetTag("airport.iata_code", airportCreatedEvent.IataCode);
        activity?.SetTag("airport.name", airportCreatedEvent.Name);
        activity?.SetTag("airport.time_zone_id", airportCreatedEvent.TimeZoneId);
        context.Logger.LogInformation($"Processing AirportCreatedEvent with ID: {airportCreatedEvent.Id}");
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Airports.AsNoTracking().AnyAsync(a => a.Id == airportCreatedEvent.AirportId))
        {
            context.Logger.LogWarning($"Airport with ID: {airportCreatedEvent.AirportId} already exists. Skipping creation.");
            return;
        }
        var clock = _host.Services.GetRequiredService<IClock>();
        var airport = Airport.Create(new AirportCreationArgs
        {
            CreatedAt = clock.GetCurrentInstant(),
            IataCode = airportCreatedEvent.IataCode,
            IcaoCode = airportCreatedEvent.IcaoCode,
            Id = airportCreatedEvent.AirportId,
            Name = airportCreatedEvent.Name,
            TimeZoneId = airportCreatedEvent.TimeZoneId
        });
        dbContext.Airports.Add(airport);
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Successfully processed AirportCreatedEvent with ID: {airportCreatedEvent.Id}");
    }
}
