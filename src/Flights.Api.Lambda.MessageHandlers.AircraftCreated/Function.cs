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

namespace Flights.Api.Lambda.MessageHandlers.AircraftCreated;

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
        using var activity = Activity.Current?.Source.StartActivity("AircraftCreatedEventHandler.ProcessMessage");
        context.Logger.LogInformation($"Processed message {message.Body}");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            context.Logger.LogWarning("Message element is null or empty.");
            return;
        }
        var aircraftCreatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AircraftCreatedEvent>();
        if (aircraftCreatedEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize AircraftCreatedEvent.");
            return;
        }
        activity?.SetTag("aircraft.id", aircraftCreatedEvent.AircraftId);
        activity?.SetTag("aircraft.tail_number", aircraftCreatedEvent.TailNumber);
        activity?.SetTag("aircraft.equipment_code", aircraftCreatedEvent.EquipmentCode);
        context.Logger.LogInformation($"Processing creation of Aircraft with ID: {aircraftCreatedEvent.AircraftId} and Tail Number: {aircraftCreatedEvent.TailNumber}. Event ID: {aircraftCreatedEvent.Id}");
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Aircraft.AsNoTracking().AnyAsync(a => a.Id == aircraftCreatedEvent.AircraftId))
        {
            context.Logger.LogWarning($"Aircraft with ID: {aircraftCreatedEvent.AircraftId} already exists. Skipping creation.");
            return;
        }
        var clock = _host.Services.GetRequiredService<IClock>();
        var aircraft = Aircraft.Create(aircraftCreatedEvent.AircraftId, aircraftCreatedEvent.TailNumber, aircraftCreatedEvent.EquipmentCode, clock.GetCurrentInstant());
        dbContext.Aircraft.Add(aircraft);
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Successfully processed AircraftCreatedEvent with ID: {aircraftCreatedEvent.Id}");
    }
}
