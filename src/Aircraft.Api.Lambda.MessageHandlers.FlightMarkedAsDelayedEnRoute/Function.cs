using System.Diagnostics;
using System.Text.Json;
using Aircraft.Infrastructure;
using Aircraft.Infrastructure.Database;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Trace;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

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
        builder.Services.AddDatabaseConnection(config);
        builder.Services.AddSingleton(TimeProvider.System);
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
        using var activity = Activity.Current?.Source.StartActivity("FlightMarkedAsDelayedEnRouteEventHandler.ProcessMessage");
        context.Logger.LogInformation($"Processed message {message.Body}");
        var messageElement = JsonDocument.Parse(message.Body).RootElement.GetProperty("Message").GetString();
        if (string.IsNullOrWhiteSpace(messageElement))
        {
            context.Logger.LogWarning("Message element is null or empty.");
            return;
        }
        var flightMarkedAsDelayedEnRouteEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<FlightMarkedAsDelayedEnRouteEvent>();
        if (flightMarkedAsDelayedEnRouteEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize flightMarkedAsDelayedEnRouteEvent.");
            return;
        }
        activity?.SetTag("flight.id", flightMarkedAsDelayedEnRouteEvent.FlightId);
        activity?.SetTag("flight.aircraft_id", flightMarkedAsDelayedEnRouteEvent.AircraftId);
        activity?.SetTag("flight.arrival_airport_icao_code", flightMarkedAsDelayedEnRouteEvent.ArrivalAirportIcaoCode);
        context.Logger.LogInformation($"Processing flightMarkedAsDelayedEnRouteEvent with ID: {flightMarkedAsDelayedEnRouteEvent.Id}");
        using var scope = _host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aircraft = await dbContext.Aircraft.FindAsync(flightMarkedAsDelayedEnRouteEvent.AircraftId);
        if (aircraft is null)
        {
            context.Logger.LogWarning($"Aircraft with ID {flightMarkedAsDelayedEnRouteEvent.AircraftId} not found.");
            return;
        }
        var timeProvider = _host.Services.GetRequiredService<TimeProvider>();
        aircraft.MarkAsEnRoute(flightMarkedAsDelayedEnRouteEvent.ArrivalAirportIcaoCode, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Aircraft with ID {aircraft.Id} marked as EnRoute to {aircraft.EnRouteTo}.");
    }
}
