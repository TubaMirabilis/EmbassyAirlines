using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Flights.Core.Models;
using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Flights.Api.Lambda.MessageHandlers.AircraftCreated;

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
        services.AddSingleton<IClock>(SystemClock.Instance);
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
                context.Logger.LogError($"Error processing message {message.MessageId}: {ex.Message}");
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
        var aircraftCreatedEvent = JsonDocument.Parse(messageElement).RootElement.GetProperty("data").Deserialize<AircraftCreatedEvent>();
        if (aircraftCreatedEvent is null)
        {
            context.Logger.LogWarning("Failed to deserialize AircraftCreatedEvent.");
            return;
        }
        context.Logger.LogInformation($"Processing AircraftCreatedEvent with ID: {aircraftCreatedEvent.Id}");
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Aircraft.AsNoTracking().AnyAsync(a => a.Id == aircraftCreatedEvent.AircraftId))
        {
            context.Logger.LogWarning($"Aircraft with ID: {aircraftCreatedEvent.AircraftId} already exists. Skipping creation.");
            return;
        }
        var clock = _serviceProvider.GetRequiredService<IClock>();
        var aircraft = Aircraft.Create(aircraftCreatedEvent.AircraftId, aircraftCreatedEvent.TailNumber, aircraftCreatedEvent.EquipmentCode, clock.GetCurrentInstant());
        dbContext.Aircraft.Add(aircraft);
        await dbContext.SaveChangesAsync();
        context.Logger.LogInformation($"Successfully processed AircraftCreatedEvent with ID: {aircraftCreatedEvent.Id}");
    }
}
