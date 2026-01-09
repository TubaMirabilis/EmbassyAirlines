using System.Text.Json;
using Aircraft.Infrastructure;
using Aircraft.Infrastructure.Database;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    public Function()
    {
        var config = new ConfigurationBuilder().AddEnvironmentVariables(prefix: "FLIGHTS_").Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddDatabaseConnection(config);
        services.AddSingleton(TimeProvider.System);
        _serviceProvider = services.BuildServiceProvider();
    }
    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        var failures = new List<SQSBatchResponse.BatchItemFailure>();
        foreach (var message in evnt.Records)
        {
            try
            {
                await ProcessMessageAsync(message, context);
            }
            catch (Exception ex)
            {
                failures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = message.MessageId
                });
                context.Logger.LogError($"Error processing message {message.MessageId}: {ex}");
            }
        }
        return new SQSBatchResponse
        {
            BatchItemFailures = failures
        };
    }
    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
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
        context.Logger.LogInformation($"Processing flightMarkedAsDelayedEnRouteEvent with ID: {flightMarkedAsDelayedEnRouteEvent.Id}");
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aircraft = await dbContext.Aircraft.FindAsync(flightMarkedAsDelayedEnRouteEvent.AircraftId);
        if (aircraft is null)
        {
            context.Logger.LogWarning($"Aircraft with ID {flightMarkedAsDelayedEnRouteEvent.AircraftId} not found.");
            return;
        }
        var timeProvider = _serviceProvider.GetRequiredService<TimeProvider>();
        aircraft.MarkAsEnRoute(flightMarkedAsDelayedEnRouteEvent.ArrivalAirportIcaoCode, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Aircraft with ID {aircraft.Id} marked as EnRoute to {aircraft.EnRouteTo}.");
    }
}
